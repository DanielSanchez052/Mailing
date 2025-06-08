using System;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace Mailing.Lambda.Core.Bus;


public class SQSQueueService : IBusService
{
  private readonly IAmazonSQS _sqsClient;
  private readonly ILogger<SQSQueueService> _logger;
  private readonly string _queueUrl;

  public SQSQueueService(IAmazonSQS sqsClient, ILogger<SQSQueueService> logger, string queueUrl)
  {
    _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    Console.WriteLine($"SQS Queue URL: {_queueUrl}");
  }

  public async Task<QueueMessageResponse> SendMessageAsync<T>(T message)
  {
    try
    {
      var messageBody = JsonSerializer.Serialize(message);
      var sendMessageRequest = new SendMessageRequest
      {
        QueueUrl = _queueUrl,
        MessageBody = messageBody,
        DelaySeconds = 0 // Puedes ajustar esto si necesitas un retardo
      };

      var responseBus = await _sqsClient.SendMessageAsync(sendMessageRequest);
      _logger.LogInformation($"Mensaje enviado a SQS. MessageId: {responseBus.MessageId}");
      return new QueueMessageResponse
      {
        IsSuccess = responseBus.HttpStatusCode == System.Net.HttpStatusCode.OK,
        StatusCode = responseBus.HttpStatusCode.ToString(),
        MessageId = responseBus.MessageId
      };
    }
    catch (AmazonSQSException ex)
    {
      _logger.LogError(ex, $"Error al enviar mensaje a SQS: {ex.Message}");
      throw; // Re-lanza la excepción para que el llamador pueda manejarla
    }
  }

}
