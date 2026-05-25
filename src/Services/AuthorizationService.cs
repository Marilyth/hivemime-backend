using Microsoft.EntityFrameworkCore;

public class AuthorizationService(HiveMimeContext context)
{
    public async Task VerifyViewHiveUsersAsync(int userId, int hiveId)
    {
        if (!await context.HiveUsers.AnyAsync(h => h.HiveId == hiveId && h.UserId == userId && h.Role >= MemberRole.Moderator))
            throw new UnauthorizedAccessException("You do not have permission to view hive users.");
    }

    public async Task VerifyModifyHiveUserAsync(int assignerId, int hiveUserId, MemberRole role)
    {
        // Only allow assigning roles lower than the assigner's role and the assigner must have a role higher than the user being modified.
        if (!await context.HiveUsers.AnyAsync(h => h.Id == hiveUserId &&
            h.Hive.Users.Any(u => u.UserId == assignerId && u.Role > role && u.Role > h.Role)))
            throw new UnauthorizedAccessException("You do not have permission to assign this role to the user.");
    }

    public async Task VerifyLeaveHiveAsync(int userId, int hiveUserId)
    {
        if (!await context.HiveUsers.AnyAsync(f => f.Id == hiveUserId && f.UserId == userId))
            throw new UnauthorizedAccessException("You do not have permission to unassign the user from the hive.");
    }

    public async Task VerifyApprovePostsAsync(int userId, int hiveId)
    {
        if (!await context.Posts.AnyAsync(p => p.HiveId == hiveId &&
            p.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator)))
            throw new UnauthorizedAccessException("You do not have permission to approve posts in this hive.");
     }

    public async Task VerifyApprovePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            p.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator)))
            throw new UnauthorizedAccessException("You do not have permission to approve this post.");
    }

    public async Task VerifyCreatePostAsync(int userId, int? hiveId)
    {
        if (hiveId == null)
            return;

        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            h.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator) || (
                (h.Settings.PostPolicy == PostPolicy.Anyone ||
                (h.Settings.PostPolicy == PostPolicy.FollowersOnly && h.Users.Any(f => f.Id == userId)) ||
                (h.Settings.PostPolicy == PostPolicy.ModeratorsOnly && h.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator))) &&
                context.Users.Where(u => u.Id == userId).Select(u => u.Honey).All(honey => honey >= h.Settings.MinHoneyToPost)
            )))
            throw new UnauthorizedAccessException("You do not have permission to create a post in this hive.");
    }

    public async Task VerifyDeletePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            (p.CreatorId == userId || p.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator))))
            throw new UnauthorizedAccessException("You do not have permission to delete this post.");
    }

    public async Task VerifyCreateCommentAsync(int userId, int postId)
    {
        if (await context.Posts.Where(p => p.Id == postId).Select(p => p.HiveId == null).FirstAsync())
            return;

        if (!await context.Posts.AnyAsync(p => p.Id == postId))
            throw new UnauthorizedAccessException("You do not have permission to create a comment in this hive.");
    }

    public async Task VerifyDeleteCommentAsync(int userId, int commentId)
    {
        if (!await context.Comments.AnyAsync(c => c.Id == commentId &&
            (c.UserId == userId || c.Post.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator))))
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");
    }

    public async Task VerifyEditHiveAsync(int userId, int hiveId)
    {
        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            h.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Admin)))
            throw new UnauthorizedAccessException("You do not have permission to edit this hive.");
    }
}