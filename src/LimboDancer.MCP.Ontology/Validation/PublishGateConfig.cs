using System;
using LimboDancer.MCP.Ontology.Runtime;

namespace LimboDancer.MCP.Ontology.Validation
{
    /// <summary>
    /// Decision helper for whether to auto-publish, keep proposed, or reject based on governance metrics.
    /// </summary>
    public sealed class PublishGateConfig
    {
        public double MinConfidenceForPublish { get; init; } = 0.85;
        public int MaxComplexityForPublish { get; init; } = 5;
        public int MaxDepthForPublish { get; init; } = 4;

        public double MinConfidenceForProposal { get; init; } = 0.5;
        public int MaxComplexityForProposal { get; init; } = 9;
        public int MaxDepthForProposal { get; init; } = 9;
    }

    public enum GateDecision
    {
        Reject,
        Propose,
        Publish
    }

    public static class PublishGates
    {
        public static GateDecision Decide(IScored scored, PublishGateConfig? cfg = null)
        {
            cfg ??= new PublishGateConfig();

            if (scored.Confidence >= cfg.MinConfidenceForPublish &&
                scored.Complexity <= cfg.MaxComplexityForPublish &&
                scored.Depth <= cfg.MaxDepthForPublish)
            {
                return GateDecision.Publish;
            }

            if (scored.Confidence >= cfg.MinConfidenceForProposal &&
                scored.Complexity <= cfg.MaxComplexityForProposal &&
                scored.Depth <= cfg.MaxDepthForProposal)
            {
                return GateDecision.Propose;
            }

            return GateDecision.Reject;
        }

        public static PublicationStatus ToStatus(GateDecision decision) => decision switch
        {
            GateDecision.Publish => PublicationStatus.Published,
            GateDecision.Propose => PublicationStatus.Proposed,
            _ => PublicationStatus.Rejected
        };
    }
}