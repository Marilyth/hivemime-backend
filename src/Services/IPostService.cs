public interface IPostService
{
    /// <summary>
    /// Fetches and returns a pre selection of hot posts to show in the browse section.
    /// </summary>
    /// <returns>The list of posts to show in the browse section.</returns>
    List<ListPostDto> BrowsePosts();

    /// <summary>
    /// Creates a new post.
    /// </summary>
    /// <param name="userId">The ID of the user creating the post.</param>
    /// <param name="postDto">The post to create.</param>
    void CreatePost(int userId, CreatePostDto postDto);

    /// <summary>
    /// Fetches and returns the details of a post, including its options and votes.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch details for.</param>
    /// <returns>The details of the specified poll.</returns>
    PollResultsDto GetPollDetails(int pollId);

    /// <summary>
    /// Inserts or updates a user's votes on a poll.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="votes">The votes to insert or update.</param>
    void UpsertVoteToPoll(int userId, UpsertVoteToPollDto[] votes);
}
