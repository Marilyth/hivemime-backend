using Microsoft.EntityFrameworkCore;

public class AuthorizationService(HiveMimeContext context)
{
    public async Task VerifyViewHiveUsersAsync(int userId, int hiveId)
    {
        if (!await context.HiveUsers.AnyAsync(h => h.HiveId == hiveId && h.UserId == userId &&
            h.Role >= MemberRole.Moderator && h.ApprovalStatus == ApprovalStatus.Approved))
            throw new UnauthorizedAccessException("You do not have permission to view hive users.");
    }

    public async Task VerifyModifyHiveUserAsync(int assignerId, int hiveUserId, MemberRole role)
    {
        List<HiveUser> users = await context.HiveUsers.Where(h => h.Id == hiveUserId || h.UserId == assignerId)
            .ToListAsync();

        HiveUser assigner = users.FirstOrDefault(u => u.UserId == assignerId);
        HiveUser target = users.FirstOrDefault(u => u.Id == hiveUserId);

        if (target == null)
            throw new ValidationException("The specified hive user does not exist.");

        if (target.Role == MemberRole.Guest && role != MemberRole.Guest)
            throw new ValidationException("Hive guests cannot be assigned a different role.");

        if (assigner.Role <= role)
            throw new UnauthorizedAccessException("You cannot assign a role equal to or higher than your own.");

        if (assigner == null ||
            assigner.ApprovalStatus != ApprovalStatus.Approved ||
            assigner.Role < MemberRole.Moderator ||
            assigner.Role <= target.Role)
            throw new UnauthorizedAccessException("You do not have permission to modify this user's role.");
    }

    public async Task VerifyBanHiveUserAsync(int assignerId, int userId, int hiveId)
    {
        List<HiveUser> users = await context.HiveUsers.Where(h => h.HiveId == hiveId && (h.UserId == assignerId || h.UserId == userId))
            .ToListAsync();

        HiveUser assigner = users.FirstOrDefault(u => u.UserId == assignerId);
        HiveUser target = users.FirstOrDefault(u => u.UserId == userId);

        if (assigner == null ||
            assigner.ApprovalStatus != ApprovalStatus.Approved ||
            assigner.Role < MemberRole.Moderator ||
            (target != null && assigner.Role <= target?.Role))
            throw new UnauthorizedAccessException("You do not have permission to ban this user from the hive.");
    }

    public async Task VerifyLeaveHiveAsync(int assignerId, int hiveUserId)
    {
        List<HiveUser> users = await context.HiveUsers.Where(h => h.Id == hiveUserId || h.UserId == assignerId)
            .ToListAsync();

        HiveUser assigner = users.FirstOrDefault(u => u.UserId == assignerId);
        HiveUser target = users.FirstOrDefault(u => u.Id == hiveUserId);

        if (assigner == null)
            throw new ValidationException("The specified assigner does not exist.");

        if (target == null)
            throw new ValidationException("The specified hive user does not exist.");

        if (assigner.UserId != target.UserId)
            throw new ValidationException("You can only leave a hive on your own behalf.");

        if (target.ApprovalStatus > ApprovalStatus.Approved)
            throw new UnauthorizedAccessException("You can not leave the hive while you are rejected or banned.");

        if (target.Role == MemberRole.Creator)
            throw new ValidationException("The creator of the hive cannot leave it.");
    }

    public async Task VerifyApprovePostsAsync(int userId, int hiveId)
    {
        if (!await context.Posts.AnyAsync(p => p.HiveId == hiveId &&
            p.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator && u.ApprovalStatus == ApprovalStatus.Approved)))
            throw new UnauthorizedAccessException("You do not have permission to approve posts in this hive.");
    }

    public async Task VerifyApprovePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId &&
            p.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator && u.ApprovalStatus == ApprovalStatus.Approved)))
            throw new UnauthorizedAccessException("You do not have permission to approve this post.");
    }

    public async Task VerifyCreatePostAsync(int userId, int? hiveId)
    {
        if (hiveId == null)
            return;

        var data = await context.HiveUsers
            .Where(h => h.HiveId == hiveId && h.UserId == userId)
            .Select(h => new
            {
                h.Role,
                h.ApprovalStatus,
                h.User.Honey,
                h.Hive.Settings.MinRoleToPost,
                h.Hive.Settings.MinHoneyToPost
            })
            .FirstOrExceptionAsync();

        if (data.ApprovalStatus == ApprovalStatus.Banned)
            throw new UnauthorizedAccessException("You have been banned from this hive.");

        if (data.MinRoleToPost != null && (data.Role < data.MinRoleToPost || data.ApprovalStatus == ApprovalStatus.Banned))
            throw new UnauthorizedAccessException(
                $"Posting on this hive requires a minimum role of {data.MinRoleToPost}.");

        if (data.Honey < data.MinHoneyToPost)
            throw new UnauthorizedAccessException(
                $"Posting on this hive requires at least {data.MinHoneyToPost} honey.");
    }

    public async Task VerifyDeletePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId && p.CreatorId == userId))
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
            (c.UserId == userId || c.Post.Hive.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Moderator && u.ApprovalStatus == ApprovalStatus.Approved))))
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");
    }

    public async Task VerifyEditHiveAsync(int userId, int hiveId)
    {
        if (!await context.Hives.AnyAsync(h => h.Id == hiveId &&
            h.Users.Any(u => u.UserId == userId && u.Role >= MemberRole.Admin && u.ApprovalStatus == ApprovalStatus.Approved)))
            throw new UnauthorizedAccessException("You do not have permission to edit this hive.");
    }
}