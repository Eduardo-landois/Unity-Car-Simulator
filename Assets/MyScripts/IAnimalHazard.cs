// Shared by AnimalCrossing and AnimalDashCrossing so EDashcamAnimalUI can
// treat any animal-hazard behavior the same way for ground-truth detection,
// regardless of which script is actually moving the animal.
public interface IAnimalHazard
{
    bool IsCrossing { get; }
}
