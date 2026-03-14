public static class PollExtensions
{
    public static Post ToPost(this CreatePostDto dto)
    {
        return new Post
        {
            Title = dto.Title,
            Description = dto.Description,
            Polls = dto.Polls.Select(pollDto => pollDto.ToPoll()).ToList()
        };
    }

    public static Poll ToPoll(this CreatePollDto dto)
    {
        return new()
        {
            Title = dto.Title,
            Description = dto.Description,
            IsShuffled = dto.IsShuffled,
            IsOptional = dto.IsOptional,
            PollType = dto.PollType,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            StepValue = dto.StepValue,
            MinVotes = dto.MinVotes,
            MaxVotes = dto.MaxVotes,
            Categories = dto.Categories.Select(category => category.ToCategory()).ToList(),
            Candidates = dto.Candidates.Select(candidate => candidate.ToCandidate()).ToList()
        };
    }

    public static PostDto ToListPostDto(this Post post)
    {
        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            Polls = post.Polls.Select(poll => poll.ToListPollDto()).ToList()
        };
    }

    public static PollDto ToListPollDto(this Poll poll)
    {
        return new PollDto
        {
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.PollType,
            IsShuffled = poll.IsShuffled,
            IsOptional = poll.IsOptional,
            MinValue = poll.MinValue,
            MaxValue = poll.MaxValue,
            StepValue = poll.StepValue,
            MinVotes = poll.MinVotes,
            MaxVotes = poll.MaxVotes,
            Categories = poll.Categories.Select(category => category.ToCategoryDto()).ToList(),
            Candidates = poll.Candidates.Select(option => option.ToCandidateDto()).ToList()
        };
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

            // Include country the vote came from.
            if (!string.IsNullOrEmpty(postVote.Country))
            {
                string country = postVote.Country;

                if (!countryPollCandidates.ContainsKey(country))
                    countryPollCandidates[country] = new Candidate { Name = country, Votes = [] };
                
                countryPollCandidates[country].Votes.Add(new CandidateVote { Value = 1 });
            }

            // Include age of the user.
            if (settings.ShareAgeOfVote && postVote.User.DateOfBirth.HasValue)
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
            if (settings.ShareDateOfVote)
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
            Polls = polls.Select(ToPollResultsDto).ToList()
        };
    }

    public static PollResultDto ToPollResultsDto(this Poll poll)
    {
        return new PollResultDto
        {
            Candidates = poll.Candidates.Select(option => option.ToCandidateResultDto()).ToList()
        };
    }
}