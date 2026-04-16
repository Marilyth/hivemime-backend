using Microsoft.EntityFrameworkCore;

public class VoteTrigger(HiveMimeContext context) : BaseTrigger<PostVote>
{
    public override async Task OnAddedAsync(PostVote entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.VoteCount, p => p.VoteCount + 1));

    public override async Task OnDeletedAsync(PostVote entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.VoteCount, p => p.VoteCount - 1));
}
