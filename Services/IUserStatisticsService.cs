using Karina.Api.DTOs.User;

namespace Karina.Api.Services;

public interface IUserStatisticsService
{

    public Task<UserStatisticsResponse> getUserStatistics(Guid user_id);

    public Task<bool> setUserPreferences(Guid user_id, UserPreferencesRequest request);

    public Task<UserPreferencesResponse> getUserPreferences(Guid user_id);

}