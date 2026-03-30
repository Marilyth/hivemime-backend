using Mapster;
using Microsoft.EntityFrameworkCore;

public static class PostExtensions
{   
    public static IQueryable<Post> IncludeForBrowse(this IQueryable<Post> query)
    {
        return query
            .Include(post => post.Polls)
                .ThenInclude(poll => poll.Categories)
            .Include(post => post.Polls)
                .ThenInclude(poll => poll.Candidates)
            .Include(post => post.Creator)
            .Include(post => post.Hive);
    }

    public static PostResultDto ToPostResultsDto(this Post post)
    {
        // Add meta data polls.
        Dictionary<string, Candidate> agePollCandidates = new();
        Dictionary<string, Candidate> datePollCandidates = new();
        Dictionary<string, Candidate> countryPollCandidates = new();

        foreach (PostVote postVote in post.PostVotes)
        {
            // The user can opt out of sharing demographic data. Check for each kind.
            UserSettings settings = postVote.User.Settings;

            // Include country of the voter.
            if (settings.ShareCountryOnVote && !string.IsNullOrEmpty(settings.Country))
            {
                string country = settings.Country;

                if (!countryPollCandidates.ContainsKey(country))
                    countryPollCandidates[country] = new Candidate { Name = country, Votes = [] };
                
                countryPollCandidates[country].Votes.Add(new CandidateVote { Value = 1 });
            }

            // Include age of the user.
            if (settings.ShareAgeOnVote && postVote.User.DateOfBirth.HasValue)
            {
                // Determine the age at the time of voting.
                int ageValue = postVote.CreatedAt.Year - postVote.User.DateOfBirth.Value.Year;

                if (postVote.CreatedAt < postVote.User.DateOfBirth.Value.AddYears(ageValue))
                    ageValue--;

                string age = ageValue.ToString();

                // Bucket it up for now.
                if (ageValue < 18)
                    age = "Under 18";
                else if (ageValue < 30)
                    age = "18-29";
                else if (ageValue < 45)
                    age = "30-44";
                else if (ageValue < 60)
                    age = "45-59";
                else
                    age = "60+";

                if (!agePollCandidates.ContainsKey(age))
                    agePollCandidates[age] = new Candidate { Name = age, Votes = [] };

                agePollCandidates[age].Votes.Add(new CandidateVote { Value = 1 });
            }
            
            // Include the date of the vote.
            if (settings.ShareDateOnVote)
            {
                string date = postVote.CreatedAt.ToString("yyyy-MM-dd");

                if (!datePollCandidates.ContainsKey(date))
                    datePollCandidates[date] = new Candidate { Name = date, Votes = [] };

                datePollCandidates[date].Votes.Add(new CandidateVote { Value = 1 });
            }
        }

        List<Poll> polls = [.. post.Polls,
            new()
            {
                Title = "Where are you from? (Automatic)",
                PollType = PollType.Choice,
                MaxVotes = 1,
                MinVotes = 1,
                Candidates = countryPollCandidates.Values.ToList()
            },
            new()
            {
                Title = "When did you vote? (Automatic)",
                PollType = PollType.Choice,
                MaxVotes = 1,
                MinVotes = 1,
                Candidates = datePollCandidates.Values.ToList()
            },
            new()
            {   
                Title = "What is your age group? (Automatic)",
                PollType = PollType.Choice,
                MaxVotes = 1,
                MinVotes = 1,
                Candidates = agePollCandidates.Values.ToList()
            }
        ];

        return new PostResultDto
        {
            Polls = polls.Adapt<List<PollResultDto>>()
        };
    }

    public static PollResultDto ToPollResultsDto(this Poll poll)
    {
        return poll.Adapt<PollResultDto>();
    }
}