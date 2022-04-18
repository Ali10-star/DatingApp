using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController: BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;

        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUserName();
            if (username == createMessageDto.RecipientUsername.ToLower())
                return BadRequest("You cannot send a message to yourself.");

            var sender = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null)
                return NotFound("Recipient not found.");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content,
            };

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Complete())
                return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser( [FromQuery]
            MessageParams messageParams)
        {
            messageParams.Username = User.GetUserName();
            var messages = await _unitOfWork.MessageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                    messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int messageId)
        {
            var username = User.GetUserName();
            var message = await _unitOfWork.MessageRepository.GetMessage(messageId);

            if (message.Sender.UserName != username && message.Recipient.UserName != username)
                return Unauthorized();

            // The sender is deleting
            if (message.Sender.UserName == username)
                message.SenderDeleted = true;

            // The recipient is deleting
            if (message.Recipient.UserName == username)
                message.RecipientDeleted = true;

            if (message.SenderDeleted && message.RecipientDeleted)
                _unitOfWork.MessageRepository.DeleteMessage(message);

            if (await _unitOfWork.Complete())
                return Ok();

            return BadRequest("Problem deleting the message.");
        }
    }
}