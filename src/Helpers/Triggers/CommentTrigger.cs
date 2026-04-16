using Microsoft.EntityFrameworkCore;

public class CommentTrigger(HiveMimeContext context) : BaseTrigger<Comment>
{
    public override async Task OnAddedAsync(Comment entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.CommentCount, p => p.CommentCount + 1));

    public override async Task OnDeletedAsync(Comment entity)
        => await context.Posts.Where(p => p.Id == entity.PostId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.CommentCount, p => p.CommentCount - 1));
}
