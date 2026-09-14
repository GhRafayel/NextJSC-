
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class FriendsService(AppDbContext db, TokenService token) : ApiServiceBase(db, token)
{

    private async Task<Friends?> ExistsFriend (int senderId, int receiverId)
    {
        return await DB.Friends.Include(f => f.Sender).Include(f => f.Receiver).FirstOrDefaultAsync(fr =>  
            (fr.SenderId == senderId && fr.ReceiverId == receiverId) ||  
            (fr.SenderId == receiverId && fr.ReceiverId == senderId)
        );
    }

    private async Task<Friends?> GetReceiverPendingFriend (int senderId, int receiverId)
    {
        return await DB.Friends.Include(f => f.Sender).Include(f => f.Receiver).FirstOrDefaultAsync(fr => 
            fr.ReceiverId == senderId && fr.SenderId == receiverId && fr.Status == FriendStatus.PENDING);
    }
    public async Task<List<Friends>> GetFriends(int senderId)
    {
        return await DB.Friends.Include(f => f.Sender).Include(f => f.Receiver)
            .Where(friend =>
               (friend.ReceiverId == senderId || friend.SenderId == senderId) && friend.Status != FriendStatus.REJECTED
            ).ToListAsync();
    }

    public async Task<bool> DeleteFriend(int senderId, Guid friendId)
    {
        return await DB.Friends.Where(f => f.Id == friendId && (f.SenderId == senderId || f.ReceiverId == senderId))
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<Friends?> InviteFriend(int senderId, int receiverId)
    {
        Friends? friends = await ExistsFriend(senderId, receiverId);
        if (friends is not null)
            return null;
        await AddFriend(senderId, receiverId);
        return await ExistsFriend(senderId, receiverId);
    }
    
    public async Task<Friends?> AcceptFriend(int senderId, int receiverId)
    {
        Friends? friends = await GetReceiverPendingFriend(senderId, receiverId);
        if (friends is null)
            return null;

        Console.WriteLine("****************\n\n");

        friends.Status = FriendStatus.ACCEPTED;
        await DB.SaveChangesAsync();
        return friends;
    }

    public async Task<Friends?> RejectFriend(int senderId, int receiverId)
    {
        Friends? friend = await GetReceiverPendingFriend(senderId, receiverId);
        if (friend is null)
            return null;
        await DeleteFriend(senderId, friend.Id);
        return friend;
    }
    
    public async Task<bool?> CancelInvitation(int senderId, int receiverId)
    {
        Friends? friend = await DB.Friends.FirstOrDefaultAsync(fr =>
            fr.SenderId == senderId && fr.ReceiverId == receiverId && fr.Status == FriendStatus.PENDING);
        if (friend is null)
            return null;
        return await DeleteFriend(senderId, friend.Id);
    }
}