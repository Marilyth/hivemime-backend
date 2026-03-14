public interface IPostService
{
    /// <summary>
    /// Fetches and returns a pre selection of hot posts to show in the browse section.
    /// </summary>
    /// <returns>The list of posts to show in the browse section.</returns>
    /// <param name="userId">The ID of the user browsing posts, for individual feeds.</param>
    /// <param name="filter">The filter to apply to the posts.</param>
    List<PostDto> BrowsePosts(int userId, string filter);

    /// <summary>
    /// Creates a new post.
    /// </summary>
    /// <param name="userId">The ID of the user creating the post.</param>
    /// <param name="postDto">The post to create.</param>
    void CreatePost(int userId, CreatePostDto postDto);

    /// <summary>
    /// Fetches and returns the results of a post, including all its polls.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch details for.</param>
    /// <param name="filter">The filter to apply to the post details.</param>
    PostResultDto GetPostResult(int postId, string filter);

    /// <summary>
    /// Inserts or updates a user's votes on a post.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="vote">The vote to insert or update.</param>
    /// <param name="country">The country of the user voting, for analytics.</param>
    void VoteOnPost(int userId, VoteOnPostDto vote, string country);
}
