using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LimboDancer.MCP.Graph.CosmosGremlin;
using Xunit;

namespace LimboDancer.MCP.Graph.CosmosGremlin.Tests;

public class GremlinOptionsTests
{
    [Fact]
    public void IsCosmos_Should_Return_True_For_Cosmos_Host()
    {
        var options = new GremlinOptions
        {
            Host = "myaccount.gremlin.cosmos.azure.com"
        };

        options.IsCosmos.Should().BeTrue();
    }

    [Fact]
    public void IsCosmos_Should_Return_False_For_Non_Cosmos_Host()
    {
        var options = new GremlinOptions
        {
            Host = "localhost"
        };

        options.IsCosmos.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Throw_When_Required_Fields_Missing()
    {
        var options = new GremlinOptions();

        var action = () => options.Validate();

        action.Should().Throw<ValidationException>()
            .WithMessage("Host is required.");
    }

    [Fact]
    public void Validate_Should_Throw_When_Cosmos_Uses_Wrong_Port()
    {
        var options = new GremlinOptions
        {
            Host = "myaccount.gremlin.cosmos.azure.com",
            Port = 8182,
            Database = "testdb",
            Graph = "testgraph",
            AuthKey = "testkey"
        };

        var action = () => options.Validate();

        action.Should().Throw<ValidationException>()
            .WithMessage("Cosmos Gremlin requires Port = 443.");
    }

    [Fact]
    public void Validate_Should_Throw_When_Cosmos_Disables_SSL()
    {
        var options = new GremlinOptions
        {
            Host = "myaccount.gremlin.cosmos.azure.com",
            Port = 443,
            EnableSsl = false,
            Database = "testdb",
            Graph = "testgraph",
            AuthKey = "testkey"
        };

        var action = () => options.Validate();

        action.Should().Throw<ValidationException>()
            .WithMessage("Cosmos Gremlin requires TLS (EnableSsl = true).");
    }

    [Fact]
    public void Validate_Should_Throw_When_Cosmos_Uses_GraphSON3()
    {
        var options = new GremlinOptions
        {
            Host = "myaccount.gremlin.cosmos.azure.com",
            Port = 443,
            EnableSsl = true,
            Database = "testdb",
            Graph = "testgraph",
            AuthKey = "testkey",
            Serializer = GraphSonVersion.GraphSON3
        };

        var action = () => options.Validate();

        action.Should().Throw<ValidationException>()
            .WithMessage("Cosmos Gremlin supports GraphSON 2.x only. Set Serializer = GraphSonVersion.GraphSON2.");
    }

    [Fact]
    public void Validate_Should_Pass_For_Valid_Cosmos_Configuration()
    {
        var options = new GremlinOptions
        {
            Host = "myaccount.gremlin.cosmos.azure.com",
            Port = 443,
            EnableSsl = true,
            Database = "testdb",
            Graph = "testgraph",
            AuthKey = "testkey",
            Serializer = GraphSonVersion.GraphSON2
        };

        var action = () => options.Validate();

        action.Should().NotThrow();
    }

    [Fact]
    public void ToString_Should_Redact_AuthKey()
    {
        var options = new GremlinOptions
        {
            Host = "test.example.com",
            Port = 443,
            EnableSsl = true,
            Database = "testdb",
            Graph = "testgraph",
            AuthKey = "secretkey123"
        };

        var result = options.ToString();

        result.Should().Contain("AuthKey=****");
        result.Should().NotContain("secretkey123");
        result.Should().Contain("Host=test.example.com");
        result.Should().Contain("Database=testdb");
        result.Should().Contain("Graph=testgraph");
    }

    [Fact]
    public void ParseConnectionString_Should_Parse_Valid_Connection_String()
    {
        var connectionString = "Host=myaccount.gremlin.cosmos.azure.com;Port=443;EnableSsl=true;Database=testdb;Graph=testgraph;AuthKey=testkey;Serializer=GraphSON2";

        var options = GremlinOptions.ParseConnectionString(connectionString);

        options.Host.Should().Be("myaccount.gremlin.cosmos.azure.com");
        options.Port.Should().Be(443);
        options.EnableSsl.Should().BeTrue();
        options.Database.Should().Be("testdb");
        options.Graph.Should().Be("testgraph");
        options.AuthKey.Should().Be("testkey");
        options.Serializer.Should().Be(GraphSonVersion.GraphSON2);
    }

    [Fact]
    public void ParseConnectionString_Should_Support_Legacy_Aliases()
    {
        var connectionString = "Host=test.example.com;UseSsl=true;PrimaryKey=testkey;Pool=16";

        var options = GremlinOptions.ParseConnectionString(connectionString);

        options.EnableSsl.Should().BeTrue();
        options.AuthKey.Should().Be("testkey");
        options.ConnectionPoolSize.Should().Be(16);
    }

    [Fact]
    public void TryParseConnectionString_Should_Return_False_For_Invalid_String()
    {
        var connectionString = "InvalidConnectionString";

        var result = GremlinOptions.TryParseConnectionString(connectionString, out var options, out var error);

        result.Should().BeFalse();
        options.Should().BeNull();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseConnectionString_Should_Throw_For_Invalid_Port()
    {
        var connectionString = "Host=test.com;Port=invalid;Database=db;Graph=graph;AuthKey=key";

        var action = () => GremlinOptions.ParseConnectionString(connectionString);

        action.Should().Throw<FormatException>()
            .WithMessage("Invalid Port.");
    }

    [Fact]
    public void Legacy_Properties_Should_Work()
    {
        var options = new GremlinOptions();

        // Test legacy property setters
        options.UseSsl = false;
        options.PrimaryKey = "testkey";

        // Verify they map to the new properties
        options.EnableSsl.Should().BeFalse();
        options.AuthKey.Should().Be("testkey");

        // Test legacy property getters
        options.EnableSsl = true;
        options.AuthKey = "newkey";

        options.UseSsl.Should().BeTrue();
        options.PrimaryKey.Should().Be("newkey");
    }
}