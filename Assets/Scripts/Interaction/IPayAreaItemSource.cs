namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Contrato futuro para un NPC (u otro sistema) que deja productos en el mostrador de cobro.
    /// </summary>
    public interface IPayAreaItemSource
    {
        /// <summary>Coloca en <paramref name="payArea"/> los ítems que correspondan (p. ej. desde inventario).</summary>
        bool TryDeliverItemsTo(PayArea payArea);
    }
}
