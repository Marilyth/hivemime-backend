using Microsoft.EntityFrameworkCore;

public class AuthorizationService(HiveMimeContext context)
{
    public async Task VerifyAddModeratorAsync(int userId, int hiveId)
    {
        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            (h.CreatorId == userId || h.Moderators.Any(m => m.Id == userId))))
            throw new UnauthorizedAccessException("You do not have permission to add a moderator.");
    }

    public async Task VerifyApproveFollowRequestAsync(int userId, int followRequestId)
    {
        if (!await context.HiveFollowers.AnyAsync(r => r.Id == followRequestId &&
            (r.Hive.CreatorId == userId || r.Hive.Moderators.Any(m => m.Id == userId))))
            throw new UnauthorizedAccessException("You do not have permission to approve this follow request.");
    }

    public async Task VerifyApprovePostsAsync(int userId, int hiveId)
    {
        if (!await context.Posts.AnyAsync(p => p.HiveId == hiveId &&
            (p.Hive.Moderators.Any(m => m.Id == userId) || p.Hive.CreatorId == userId)))
            throw new UnauthorizedAccessException("You do not have permission to approve posts in this hive.");
     }

    public async Task VerifyApprovePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            (p.Hive.Moderators.Any(m => m.Id == userId) || p.Hive.CreatorId == userId)))
            throw new UnauthorizedAccessException("You do not have permission to approve this post.");
    }

    public async Task VerifyCreatePostAsync(int userId, int? hiveId)
    {
        if (hiveId == null)
            return;

        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            (h.CreatorId == userId || h.Moderators.Any(m => m.Id == userId)) || (
                (h.Settings.PostPolicy == PostPolicy.Anyone ||
                (h.Settings.PostPolicy == PostPolicy.FollowersOnly && h.Followers.Any(f => f.Id == userId)) ||
                (h.Settings.PostPolicy == PostPolicy.ModeratorsOnly && h.Moderators.Any(m => m.Id == userId))) &&
                context.Users.Where(u => u.Id == userId).Select(u => u.Honey).All(honey => honey >= h.Settings.MinHoneyToPost)
            )))
            throw new UnauthorizedAccessException("You do not have permission to create a post in this hive.");
    }

    public async Task VerifyDeletePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            (p.CreatorId == userId || p.Hive.Moderators.Any(m => m.Id == userId) || p.Hive.CreatorId == userId)))
            throw new UnauthorizedAccessException("You do not have permission to delete this post.");
    }

    public async Task VerifyCreateCommentAsync(int userId, int postId)
    {
        if (await context.Posts.Where(p => p.Id == postId).Select(p => p.HiveId == null).FirstAsync())
            return;

        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            (p.Hive.Moderators.Any(m => m.Id == userId) || p.Hive.CreatorId == userId)))
            throw new UnauthorizedAccessException("You do not have permission to create a comment in this hive.");
    }

    public async Task VerifyDeleteCommentAsync(int userId, int commentId)
    {
        if (!await context.Comments.AnyAsync(c => c.Id == commentId &&
            (c.UserId == userId || c.Post.Hive.Moderators.Any(m => m.Id == userId) || c.Post.Hive.CreatorId == userId)))
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");
    }

    public async Task VerifyEditHiveAsync(int userId, int hiveId)
    {
        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            (h.CreatorId == userId || h.Moderators.Any(m => m.Id == userId))))
            throw new UnauthorizedAccessException("You do not have permission to edit this hive.");
    }
}