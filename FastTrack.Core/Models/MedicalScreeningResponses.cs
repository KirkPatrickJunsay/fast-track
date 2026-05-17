namespace FastTrack.Models;

/// <summary>
/// Self-report responses to the fasting safety screening (US-06.4).
/// Any 'true' flag routes the user into educational mode permanently
/// until they revisit and clear it (with the exception of eating disorder
/// history, which is non-reversible per the spec).
/// </summary>
public sealed class MedicalScreeningResponses
{
    public bool PregnantOrBreastfeeding { get; set; }
    public bool Under18 { get; set; }
    public bool EatingDisorderHistory { get; set; }
    public bool InsulinDependentDiabetes { get; set; }
    public bool TakesFoodDependentMedications { get; set; }
    public bool OtherChronicCondition { get; set; }

    public bool AnyContraindicated =>
        PregnantOrBreastfeeding ||
        Under18 ||
        EatingDisorderHistory ||
        InsulinDependentDiabetes ||
        TakesFoodDependentMedications ||
        OtherChronicCondition;
}
