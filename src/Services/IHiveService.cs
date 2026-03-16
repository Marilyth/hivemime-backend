public interface IHiveService
{
    /// <summary>
    /// Fetches and returns hives depending on the provided filter and pagination parameters.
    /// </summary>
    /// <param name="afterId">The ID of the last hive seen, for pagination.</param>
    /// <param name="filter">The filter to apply to the hives, based on their name.</param>
    List<HiveDto> BrowseHives(int? afterId, string filter);

    /// <summary>
    /// Creates and returns a new hive based on the provided data.
    /// </summary>
    /// <param name="userId">The ID of the user creating the hive.</param>
    /// <param name="hiveDto">The data for the new hive.</param>
    /// <returns>The created hive.</returns>
    HiveDto CreateHive(int userId, CreateHiveDto hiveDto);
    
    /// <summary>
    /// Fetches and returns a hive by its ID.
    /// </summary>
    /// <param name="hiveId">The ID of the hive to fetch.</param>
    /// <returns>The hive with the specified ID.</returns>
    HiveDto GetHiveById(int hiveId);
}
