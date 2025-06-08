using System;
using System.Text.Json;
using Mailing.Lambda.Core.Mailing.Models;

namespace Mailing.Lambda.Core.Mailing.Validators;

public class MailingRequestValidator
{
  public static Dictionary<string, string> Validate(MailingRequest request)
  {
    var errors = new Dictionary<string, string>();

    if (request == null)
    {
      errors["Request"] = "Request cannot be null.";
      return errors;
    }

    if (request.Recipients == null || request.Recipients.Count == 0)
      errors["Recipients"] = "At least one recipient is required.";
    else
    {
      bool hasToRecipient = false;

      for (int i = 0; i < request.Recipients.Count; i++)
      {
        var recipient = request.Recipients[i].Email;
        var recipientType = request.Recipients[i].Type.Trim().ToLower();
        var recipientTypeErrors = RecipientValidator.Validate(recipient, recipientType, i);

        foreach (var error in recipientTypeErrors)
        {
          errors[error.Key] = error.Value;
        }

        if (!string.IsNullOrEmpty(recipientType) && recipientType.Equals(RecipientType.To.ToString().ToLower(), StringComparison.OrdinalIgnoreCase))
        {
          hasToRecipient = true;
        }
      }

      if (!hasToRecipient)
      {
        errors["Recipients"] = "At least one recipient of type 'To' is required.";
      }
    }

    if (string.IsNullOrWhiteSpace(request.Subject))
      errors["Subject"] = "Subject is required.";

    if (string.IsNullOrWhiteSpace(request.Body))
      errors["Body"] = "Body is required.";

    return errors;
  }
}

public class RecipientValidator
{
  public static Dictionary<string, string> Validate(string recipient, string recipientType, int index = 0)
  {
    var errors = new Dictionary<string, string>();
    List<string> recipientTypes = [.. Enum.GetNames<RecipientType>().Select(s => s.ToLower())];

    if (string.IsNullOrWhiteSpace(recipient))
      errors[$"Recipients[{index}]"] = "Recipient is required.";
    else if (!IsValidEmail(recipient))
      errors[$"Recipients[{index}]"] = "Recipient must be a valid email address.";

    if (string.IsNullOrWhiteSpace(recipientType))
      errors[$"RecipientTypes[{index}]"] = "Recipient type is required.";
    else if (!recipientTypes.Contains(recipientType))
      errors[$"RecipientTypes[{index}]"] = $"Recipient type '{recipientType}' is invalid.";

    return errors;
  }

  private static bool IsValidEmail(string email)
  {
    try
    {
      var addr = new System.Net.Mail.MailAddress(email);
      return addr.Address == email;
    }
    catch
    {
      return false;
    }
  }
}