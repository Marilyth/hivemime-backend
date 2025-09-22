using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context) : IPostService
{
    public List<ListPostDto> BrowsePosts()
    {
        List<Post> posts = context.Posts
            .Include(p => p.Polls)
                .ThenInclude(o => o.Options)
            .ToList();

        return posts.Select(p => p.ToListPostDto()).ToList();
    }

    public void CreatePost(int userId, CreatePostDto postDto)
    {
        Post newPost = postDto.ToPost();
        newPost.CreatorId = userId;

        context.Posts.Add(newPost);
        context.SaveChanges();
    }

    public PollResultsDto GetPollDetails(int pollId)
    {
        Poll poll = context.Polls
            .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
            .First(p => p.Id == pollId);

        return poll.ToPollResultsDto();
    }

    public void UpsertVoteToPoll(int userId, UpsertVoteToPollDto[] votes)
    {
        foreach (UpsertVoteToPollDto vote in votes)
        {
            // Either update the vote if one already exists, or create a new one.
            Vote dbVote = context.Votes
                .FirstOrDefault(o => o.PollOptionId == vote.PollOptionId && o.UserId == userId);

            if (dbVote is null)
            {
                dbVote = new Vote
                {
                    PollOptionId = vote.PollOptionId,
                    UserId = userId
                };

                context.Votes.Add(dbVote);
            }

            dbVote.Value = vote.Value;
        }

        context.SaveChanges();
    }
}