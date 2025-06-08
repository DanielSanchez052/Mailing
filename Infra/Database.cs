using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Infra;

public class Database : Construct
{
    public Table ClientTable { get; set; }

    public Database(Construct scope, string id) : base(scope, id)
    {
        ClientTable = new Table(this, $"{id}-ClientTable", new TableProps
        {
            TableName = "client",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "ClientId", Type = AttributeType.STRING },
            RemovalPolicy = Amazon.CDK.RemovalPolicy.DESTROY,
        });
    }
}
