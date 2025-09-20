using Microsoft.EntityFrameworkCore;

public class PollService(HiveMimeContext context) : IPollService
{
    public List<ListPollDto> BrowsePolls()
    {
        List<Poll> polls = context.Polls
            .Include(p => p.Options)
            .Include(p => p.SubPolls)
            .Where(p => p.ParentPollId == null)
            .ToList();

        return polls.Select(p => p.ToListPollDto()).ToList();
    }

    public void CreatePoll(int userId, CreatePollDto pollDto)
    {
        Poll newPoll = pollDto.ToPoll();
        newPoll.CreatorId = userId;

        context.Polls.Add(newPoll);
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