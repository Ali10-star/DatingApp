using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _photoService = photoService;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers() {
            var users = await _userRepository.GetMembersAsync();

            return Ok(users);
        }

        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync( User.GetUserName() );

            _mapper.Map(memberUpdateDto, user);

            _userRepository.Update(user);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            // Fetch user by username
            AppUser user = await _userRepository.GetUserByUsernameAsync( User.GetUserName() );

            // Upload image to cloud service
            var result = await _photoService.AddPhotoAsync(file);

            // Upload failed for some reason
            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
            };

            // If the user has no previous photos, then this is the main one
            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }
            // Add photo to user's collection
            user.Photos.Add(photo);

            // Return uploaded photo if changes were saved successfully to database
            if (await _userRepository.SaveAllAsync())
                //return CreatedAtRoute("GetUser", _mapper.Map<PhotoDto>(photo));
                return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));

            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync( User.GetUserName() );

            // Get photo by its id
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

            if (photo == null) return BadRequest("Photo not found");

            if (photo.IsMain) return BadRequest("This is already your main photo");

            // Get original main photo and set its status to false
            var currentMain = user.Photos.FirstOrDefault(p => p.IsMain);
            if (currentMain != null) currentMain.IsMain = false;

            photo.IsMain = true;

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync( User.GetUserName() );

            // Get photo by its id
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo == null) return BadRequest("Photo not found");

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            // Delete photo from cloud service (Cloudinary)
            if (photo.PublicId != null){
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);

                if (result.Error != null)
                    return BadRequest(result.Error.Message);
            }

            // Delete photo from user's collection
            user.Photos.Remove(photo);

            if (await _userRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to delete photo");
        }

    }
}