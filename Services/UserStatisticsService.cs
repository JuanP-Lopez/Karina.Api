using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

using Karina.Api.Data;
using Karina.Api.DTOs.User;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class UserStatisticsService : IUserStatisticsService
{
    private readonly KarinaContext _context;

    public UserStatisticsService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<UserStatisticsResponse> getUserStatistics(Guid user_id)
    {
            var statistics = await _context.Database.SqlQuery<UserStatisticsResponse>(
            $"""
                SELECT 
                    COUNT(distinct m.id) as total_messages,
                    COUNT(distinct m.id) FILTER  (WHERE m.source = 1) as whatsapp_messages,
                    COUNT(distinct m.id) FILTER  (WHERE m.source = 2) as website_messages,
                    COUNT(*) FILTER (WHERE r.source = 1) as whatsapp_responses,
                    COUNT(*) FILTER (WHERE r.source = 2) as website_responses
                FROM "Messages" m
                    LEFT JOIN "Responses" r
                    on r.response_to = m.id
                WHERE m.user_id = {user_id}
            """
            ).SingleAsync();

            return statistics;
    }

    public async Task<bool> setUserPreferences(Guid user_id, UserPreferencesRequest request)
    {
        var preferences = await _context.Preferences.FirstOrDefaultAsync(u => u.user_id == user_id);

        if (preferences == null)
        {
            preferences = new Preference
            {
                user_id = user_id,
                nickname = request.nickname,
                tone = request.tone,
                language = request.language
            };

            _context.Preferences.Add(preferences);
        } else
        {
            preferences.nickname = request.nickname;
            preferences.tone = request.tone;
            preferences.language = request.language;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserPreferencesResponse> getUserPreferences(Guid user_id)
    {
        var result = await _context.Preferences.Where(u => u.user_id == user_id).Select(u => new UserPreferencesResponse
        {
            nickname = u.nickname,
            tone = u.tone,
            language = u.language
        }).FirstOrDefaultAsync();

        return result;
    }
}