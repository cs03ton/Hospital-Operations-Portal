export function countFleetPassengers(
  requesterTravels: boolean,
  selectedPersonnelCount: number,
  preservedExternalCount: number,
) {
  return (requesterTravels ? 1 : 0) + selectedPersonnelCount + preservedExternalCount;
}
