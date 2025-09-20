public interface IPollService
{
    /// <summary>
    /// Fetches and returns a pre selection of hot polls to show in the browse section.
    /// </summary>
    /// <returns>The list of polls to show in the browse section.</returns>
    List<ListPollDto> BrowsePolls();

    /// <summary>
    /// Creates a new poll.
    /// </summary>
    /// <param name="userId">The ID of the user creating the poll.</param>
    /// <param name="pollDto">The poll to create.</param>
    void CreatePoll(int userId, CreatePollDto pollDto);

    /// <summary>
    /// Fetches and returns the details of a poll, including its options and votes.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch details for.</param>
    /// <returns>The details of the specified poll.</returns>
    PollResultsDto GetPollDetails(int pollId);

    /// <summary>
    /// Inserts or updates a user's votes on poll options.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="votes">The votes to insert or update.</param>
    void UpsertVoteToPoll(int userId, UpsertVoteToPollDto[] votes);
}
