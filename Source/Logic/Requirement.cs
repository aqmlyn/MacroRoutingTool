using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.Logic;

/// <summary>
/// A requirement, which may be single or contain sub-requirements.
/// </summary>
public interface IRequirement {
    /// <returns>Whether this requirement is met.</returns>
    public bool Check();
}

//TODO YAML serialization
//single reqs should be listed as their source code
//req groups should be - Any: or - All:

public class Requirement : IRequirement {
    /// <summary>
    /// Compiled code for this single requirement.
    /// </summary>
    public NumericExpression Expression;

    public bool Check() {
        return Expression.Evaluate() != 0;
    }
}

public class RequirementGroup : IRequirement {
    /// <summary>
    /// Whether all (true) or at least one (false) of the sub-requirements must be met.
    /// </summary>
    public bool NeedAll = false;

    /// <summary>
    /// This requirement's sub-requirements.
    /// </summary>
    public List<IRequirement> Requirements;

    public bool Check() {
        foreach (IRequirement req in Requirements) {
            if (req.Check() != NeedAll) {
                return !NeedAll;
            }
        }
        return NeedAll;
    }
}