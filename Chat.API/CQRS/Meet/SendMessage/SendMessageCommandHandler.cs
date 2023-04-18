﻿using System.Security.Claims;
using Chat.API.Configs;
using Chat.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Chat.API.CQRS.Meet.SendMessage
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommandRequest, SendMessageCommandResponse>
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SendMessageCommandHandler(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SendMessageCommandResponse> Handle(SendMessageCommandRequest request,
            CancellationToken cancellationToken)
        {
            var senderId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var meetId = Entities.Meet.MeetId(request.ReceiverId, senderId);

            var meet = await _dbContext.Meets.Where(x => x.Id == meetId).FirstOrDefaultAsync();

            bool isMeet = meet is null;

            if (isMeet) meet = new();

            meet.LastMessage = request.Text;
            meet.CreatedAt = DateTime.UtcNow;

            if (isMeet)
            {
                meet.Id = meetId;
                await _dbContext.Meets.AddAsync(meet);
            }

            var message = new Message(meetId: meetId, receiverId: request.ReceiverId, senderId: senderId,
                text: request.Text);

            await _dbContext.Messages.AddAsync(message);

            await _dbContext.SaveChangesAsync();

            return new SendMessageCommandResponse();
        }
    }
}