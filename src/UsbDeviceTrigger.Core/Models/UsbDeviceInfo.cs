namespace UsbDeviceTrigger.Core.Models;

/// <summary>
/// Représente les informations d'un périphérique USB
/// </summary>
public class UsbDeviceInfo
{
    /// <summary>
    /// ID unique du périphérique Windows (ex: USB\VID_046D&PID_C52B\...)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Nom convivial du périphérique
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vendor ID (VID) - Identifiant du fabricant (ex: 046D pour Logitech)
    /// </summary>
    public string VendorId { get; set; } = string.Empty;

    /// <summary>
    /// Product ID (PID) - Identifiant du produit (ex: C52B)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Numéro de série du périphérique (peut être vide pour certains appareils)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Description Windows du périphérique
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// État actuel de connexion du périphérique
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Date et heure de la dernière détection du périphérique
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.Now;

    /// <summary>
    /// Retourne une représentation lisible du périphérique
    /// </summary>
    public override string ToString()
    {
        return $"{Name} (VID:{VendorId} PID:{ProductId})";
    }

    /// <summary>
    /// Compare deux périphériques pour vérifier s'ils sont identiques
    /// </summary>
    public bool IsSameDevice(UsbDeviceInfo other)
    {
        if (other == null) return false;

        // Comparaison par DeviceId en priorité
        if (!string.IsNullOrEmpty(DeviceId) && !string.IsNullOrEmpty(other.DeviceId))
            return DeviceId.Equals(other.DeviceId, StringComparison.OrdinalIgnoreCase);

        // Comparaison par VID/PID/Serial
        if (!string.IsNullOrEmpty(SerialNumber) && !string.IsNullOrEmpty(other.SerialNumber))
        {
            return VendorId.Equals(other.VendorId, StringComparison.OrdinalIgnoreCase) &&
                   ProductId.Equals(other.ProductId, StringComparison.OrdinalIgnoreCase) &&
                   SerialNumber.Equals(other.SerialNumber, StringComparison.OrdinalIgnoreCase);
        }

        // Comparaison par VID/PID uniquement
        return VendorId.Equals(other.VendorId, StringComparison.OrdinalIgnoreCase) &&
               ProductId.Equals(other.ProductId, StringComparison.OrdinalIgnoreCase);
    }
}
