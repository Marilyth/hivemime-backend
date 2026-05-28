using Microsoft.EntityFrameworkCore;

public class AuthorizationService(HiveMimeContext context)
{
    public async Task VerifyViewHiveUsersAsync(int userId, int hiveId)
    {
        HiveUser hiveUser = await context.HiveUsers.FirstOrExceptionAsync(h => h.HiveId == hiveId && h.UserId == userId);
        MemberRole effectiveRole = GetEffectiveRole(hiveUser.ApprovalStatus, hiveUser.Role);

        if (effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to view the users of this hive.");
    }

    public async Task VerifyModifyHiveUserAsync(int assignerId, int hiveUserId, MemberRole role)
    {
        List<HiveUser> users = await context.HiveUsers.Where(h => h.Id == hiveUserId || h.UserId == assignerId)
            .ToListAsync();

        HiveUser assigner = users.FirstOrDefault(u => u.UserId == assignerId);
        HiveUser target = users.FirstOrDefault(u => u.Id == hiveUserId);

        if (target == null)
            throw new ValidationException("The specified hive user does not exist.");

        MemberRole targetEffectiveRole = GetEffectiveRole(target?.ApprovalStatus, target?.Role, false);
        MemberRole assignerEffectiveRole = GetEffectiveRole(assigner?.ApprovalStatus, assigner?.Role);

        if (assignerEffectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to modify hive users.");

        if (assignerEffectiveRole <= targetEffectiveRole)
            throw new UnauthorizedAccessException("You cannot modify a user with an equal or higher role than your own.");

        if (role >= assignerEffectiveRole)
            throw new UnauthorizedAccessException("You cannot assign a role equal to or higher than your own.");

        if (role != target?.Role && (targetEffectiveRole == MemberRole.Guest || role == MemberRole.Guest))
            throw new ValidationException("Roles can not be changed to or from Guests.");
    }

    public async Task VerifyBanHiveUserAsync(int assignerId, int userId, int hiveId)
    {
        List<HiveUser> users = await context.HiveUsers.Where(h => h.HiveId == hiveId && (h.UserId == assignerId || h.UserId == userId))
            .ToListAsync();

        HiveUser assigner = users.FirstOrDefault(u => u.UserId == assignerId);
        HiveUser target = users.FirstOrDefault(u => u.UserId == userId);

        MemberRole targetEffectiveRole = GetEffectiveRole(target?.ApprovalStatus, target?.Role, false);
        MemberRole assignerEffectiveRole = GetEffectiveRole(assigner?.ApprovalStatus, assigner?.Role);

        if (assignerEffectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to ban users from this hive.");

        if (assignerEffectiveRole <= targetEffectiveRole)
            throw new UnauthorizedAccessException("You cannot ban a user with an equal or higher role than your own.");
    }

    public async Task VerifyLeaveHiveAsync(int assignerId, int hiveUserId)
    {
        HiveUser user = await context.HiveUsers.FirstOrExceptionAsync(u => u.Id == hiveUserId);
        MemberRole effectiveRole = GetEffectiveRole(user.ApprovalStatus, user.Role);

        if (assignerId != user.UserId)
            throw new ValidationException("You can only leave a hive on your own behalf.");

        if (effectiveRole == MemberRole.Creator)
            throw new ValidationException("The creator of the hive cannot leave it.");
    }

    public async Task VerifyJoinHiveAsync(int userId, int hiveId)
    {
        double minHoneyToJoin = await context.Hives.Where(h => h.Id == hiveId)
            .Select(h => h.Settings.MinHoneyToJoin)
            .FirstOrExceptionAsync();

        var user = await context.Users.Where(u => u.Id == userId)
            .Select(u => new {
                u.Honey,
                hiveUser = u.JoinedHives.Where(j => j.HiveId == hiveId).Select(j => new {
                    j.ApprovalStatus,
                    j.Role
                }).FirstOrDefault()
            })
            .FirstOrExceptionAsync();

        if (user.Honey < minHoneyToJoin)
            throw new UnauthorizedAccessException($"Joining this hive requires at least {minHoneyToJoin} honey.");

        if (user.hiveUser?.Role > MemberRole.Guest)
            throw new ValidationException("You have already joined this hive.");
    }

    public async Task VerifyApprovePostsAsync(int userId, int hiveId)
    {
        var user = await context.HiveUsers.Where(h => h.HiveId == hiveId && h.UserId == userId)
            .Select(h => new { h.ApprovalStatus, h.Role })
            .FirstOrExceptionAsync();

        MemberRole effectiveRole = GetEffectiveRole(user.ApprovalStatus, user.Role);

        if (effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to approve posts in this hive.");
    }

    public async Task VerifyApprovePostAsync(int userId, int postId)
    {
        var user = await context.Posts.Where(p => p.Id == postId)
            .SelectMany(p => p.Hive.Users.Where(h => h.UserId == userId))
            .Select(h => new { h.ApprovalStatus, h.Role })
            .FirstOrExceptionAsync();

        MemberRole effectiveRole = GetEffectiveRole(user.ApprovalStatus, user.Role);

        if (effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to approve this post.");
    }

    public async Task VerifyVoteOnPostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId && (p.VotingLockedAt == null || p.VotingLockedAt > DateTimeOffset.UtcNow)))
            throw new ValidationException("You can not vote on this post.");
    }

    public async Task VerifyCreatePostAsync(int userId, int? hiveId)
    {
        if (hiveId == null)
            return;

        var user = await context.Users.Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Honey,
                HiveUser = u.JoinedHives.Where(j => j.HiveId == hiveId)
                    .Select(j => new { j.ApprovalStatus, j.Role })
                    .FirstOrDefault()
            })
            .FirstOrExceptionAsync();

        var hive = await context.Hives
            .Where(h => h.Id == hiveId)
            .Select(h => new
            {
                h.Settings.MinRoleToPost,
                h.Settings.MinHoneyToPost
            })
            .FirstOrExceptionAsync();

        MemberRole effectiveRole = GetEffectiveRole(user.HiveUser?.ApprovalStatus, user.HiveUser?.Role);

        if (hive.MinRoleToPost != null && effectiveRole < hive.MinRoleToPost)
            throw new UnauthorizedAccessException(
                $"Posting on this hive requires a minimum role of {hive.MinRoleToPost}.");

        if (user.Honey < hive.MinHoneyToPost && effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException(
                $"Posting on this hive requires at least {hive.MinHoneyToPost} honey.");
    }

    public async Task VerifyDeletePostAsync(int userId, int postId)
    {
        if (!await context.Posts.AnyAsync(p => p.Id == postId && p.CreatorId == userId))
            throw new UnauthorizedAccessException("Posts can only be deleted by their creator.");
    }

    public async Task VerifyCreateCommentAsync(int userId, int postId)
    {
        var user = await context.Users.Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Honey,
                HiveUser = u.JoinedHives.Where(j => j.HiveId == context.Posts.Where(p => p.Id == postId).Select(p => p.HiveId).FirstOrDefault())
                    .Select(j => new { j.ApprovalStatus, j.Role })
                    .FirstOrDefault()
            })
            .FirstOrExceptionAsync();

        var post = await context.Posts
            .Where(p => p.Id == postId)
            .Select(p => new
            {
                MinRoleToComment = (MemberRole?)p.Hive.Settings.MinRoleToComment,
                MinHoneyToComment = (double?)p.Hive.Settings.MinHoneyToComment,
                p.CommentingLockedAt
            })
            .FirstOrExceptionAsync();

        if (user.HiveUser?.ApprovalStatus == ApprovalStatus.Banned)
            throw new UnauthorizedAccessException("You have been banned from this hive.");

        MemberRole effectiveRole = GetEffectiveRole(user.HiveUser?.ApprovalStatus, user.HiveUser?.Role);

        if (post.CommentingLockedAt != null && post.CommentingLockedAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Commenting on this post is locked.");

        if (post.MinRoleToComment != null && effectiveRole < post.MinRoleToComment)
            throw new UnauthorizedAccessException(
                $"Commenting on this hive requires a minimum role of {post.MinRoleToComment}.");

        if (user.Honey < post.MinHoneyToComment && effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException(
                $"Commenting on this hive requires at least {post.MinHoneyToComment} honey.");
    }

    public async Task VerifyDeleteCommentAsync(int userId, int commentId)
    {
        Comment comment = await context.Comments.FirstOrExceptionAsync(c => c.Id == commentId);

        if (comment.UserId == userId)
            return;

        var hiveUser = await context.Comments.Where(c => c.Id == commentId)
            .SelectMany(c => c.Post.Hive.Users.Where(h => h.UserId == userId))
            .Select(h => new { h.ApprovalStatus, h.Role })
            .FirstOrExceptionAsync();

        MemberRole effectiveRole = GetEffectiveRole(hiveUser.ApprovalStatus, hiveUser.Role);

        if (effectiveRole < MemberRole.Moderator)
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");
    }

    public async Task VerifyEditHiveAsync(int userId, int hiveId)
    {
        HiveUser hiveUser = await context.HiveUsers.FirstOrExceptionAsync(h => h.HiveId == hiveId && h.UserId == userId);
        MemberRole effectiveRole = GetEffectiveRole(hiveUser.ApprovalStatus, hiveUser.Role);

        if (effectiveRole < MemberRole.Admin)
            throw new UnauthorizedAccessException("You need to be at least an admin to edit this hive.");
    }

    private MemberRole GetEffectiveRole(ApprovalStatus? approvalStatus, MemberRole? role, bool throwOnBan = true)
    {
        if (approvalStatus == ApprovalStatus.Banned && throwOnBan)
            throw new UnauthorizedAccessException("You have been banned from this hive.");

        if (role is null || approvalStatus != ApprovalStatus.Approved)
            return MemberRole.Guest;

        return role.Value;
    }
}