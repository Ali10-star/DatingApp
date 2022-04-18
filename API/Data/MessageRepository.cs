using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddGroup(Group group) => _context.Groups.Add(group);

        public void AddMessage(Message message) => _context.Messages.Add(message);

        public void DeleteMessage(Message message) => _context.Messages.Remove(message);

        public async Task<Connection> GetConnection(string connectionId) =>
            await _context.Connections.FindAsync(connectionId);

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _context.Groups.Include(group => group.Connections)
                .Where(g => g.Connections.Any(c => c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<Group> GetMessageGroup(string groupName) =>
            await _context.Groups
                  .Include(x => x.Connections).FirstOrDefaultAsync(x => x.Name == groupName);

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
                .OrderByDescending(m => m.MessageSent) // Order by most recent
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(user => user.RecipientUsername == messageParams.Username && user.RecipientDeleted == false),
                "Outbox" => query.Where(user => user.SenderUsername == messageParams.Username && user.SenderDeleted == false),
                _ => query.Where(user => user.RecipientUsername == messageParams.Username
                     && user.RecipientDeleted == false
                     && user.DateRead == null )
            };

            return await PagedList<MessageDto>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await _context.Messages
                .Where(message => (message.Recipient.UserName == currentUsername && message.RecipientDeleted == false
                    && message.Sender.UserName == recipientUsername)
                    || (message.Recipient.UserName == recipientUsername
                    && message.Sender.UserName == currentUsername && message.SenderDeleted == false)
                )
                .OrderBy(message => message.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(mDto => mDto.DateRead == null
                && mDto.RecipientUsername == currentUsername).ToList();

            if (unreadMessages.Any()) {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
                // The repository doesn't save changes to the DB
                // instead, the UnitOfWork should do this
            }

            return messages;
        }

        public void RemoveConnecion(Connection connection) =>
            _context.Connections.Remove(connection);

        public void RemoveConnection(Connection connection)
        {
            throw new NotImplementedException();
        }
    }
}