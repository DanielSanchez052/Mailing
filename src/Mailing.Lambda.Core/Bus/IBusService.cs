using System;

namespace Mailing.Lambda.Core.Bus;

public interface IBusService
{
  Task<QueueMessageResponse> SendMessageAsync<T>(T message);
}
