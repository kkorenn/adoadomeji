using Adomeji.Pets.Surfaces;

namespace Adomeji.UI.Services;

internal sealed class PetCustomizationService : IPetCustomizationService
{
  public void ApplySelectedPacks() => PetFlockController.ApplyPacksNow();
  public void RerollPets() => PetFlockController.RerollAll();
}
