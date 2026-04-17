using Microsoft.EntityFrameworkCore;

public class CommentTrigger(HiveMimeContext context) : BaseTrigger<Comment>
{
    public override async Task OnAddedAsync(Comment entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.UpdateHotness(commentDifference: 1));

    public override async Task OnDeletedAsync(Comment entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.UpdateHotness(commentDifference: -1));
}
