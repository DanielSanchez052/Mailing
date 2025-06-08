using System;

namespace Mailing.Lambda.Core.Bus;

public class QueueMessageResponse
{
  public bool IsSuccess { get; set; }
  public string StatusCode { get; set; } = default!;
  public string MessageId { get; set; } = default!;
}
