using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context) : IPostService
{
    public List<ListPostDto> BrowsePosts(int userId)
    {
        List<Post> posts = GetSuggestedPosts(userId);
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
            .Include(p => p.Candidates)
                .ThenInclude(o => o.Votes)
            .First(p => p.Id == pollId);

        return poll.ToPollResultsDto();
    }

    public void UpsertVoteToPost(int userId, UpsertVoteToPostDto vote)
    {
        foreach (UpsertVoteToPollDto pollVote in vote.Polls)
        {
            foreach (UpsertVoteToCandidateDto candidateVote in pollVote.Candidates)
            {
                // Either update the vote if one already exists, or create a new one.
                Vote dbVote = context.Votes
                    .FirstOrDefault(o => o.PollOptionId == candidateVote.CandidateId && o.UserId == userId);

                bool wasVoted = dbVote is not null;

                if (dbVote is null)
                {
                    if (!wasVoted)
                        return;

                    dbVote = new Vote
                    {
                        PollOptionId = candidateVote.CandidateId,
                        UserId = userId
                    };

                    context.Votes.Add(dbVote);
                }

                if (!wasVoted)
                {
                    // If the value is null, it means the user did not vote for this option, so we remove any existing vote.
                    context.Votes.Remove(dbVote);
                    continue;
                }

                dbVote.Value = candidateVote.Value.Value;
            }
        }

        context.SaveChanges();
    }

    private List<Post> GetSuggestedPosts(int userId)
    {
        List<Post> posts = context.Posts.Include(p => p.Polls)
                                            .ThenInclude(o => o.Candidates)
                                        .Include(p => p.Polls)
                                            .ThenInclude(o => o.Categories)
                                        .OrderByDescending(p => p.CreatedAt)
                                        .Take(20)
                                        .ToList();

        return posts;
    }
}