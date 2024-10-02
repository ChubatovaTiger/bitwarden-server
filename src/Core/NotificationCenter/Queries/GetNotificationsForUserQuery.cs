﻿#nullable enable
using Bit.Core.Context;
using Bit.Core.Exceptions;
using Bit.Core.NotificationCenter.Entities;
using Bit.Core.NotificationCenter.Models.Filter;
using Bit.Core.NotificationCenter.Queries.Interfaces;
using Bit.Core.NotificationCenter.Repositories;
using Bit.Core.Utilities;

namespace Bit.Core.NotificationCenter.Queries;

public class GetNotificationsForUserQuery : IGetNotificationsForUserQuery
{
    private readonly ICurrentContext _currentContext;
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsForUserQuery(ICurrentContext currentContext,
        INotificationRepository notificationRepository)
    {
        _currentContext = currentContext;
        _notificationRepository = notificationRepository;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdStatusFilterAsync(NotificationStatusFilter statusFilter)
    {
        if (!_currentContext.UserId.HasValue)
        {
            throw new NotFoundException();
        }

        var clientType = DeviceTypes.ToClientType(_currentContext.DeviceType);

        // Note: only returns the user's notifications - no authorization check needed
        return await _notificationRepository.GetByUserIdAndStatusAsync(_currentContext.UserId.Value, clientType,
            statusFilter);
    }
}
