namespace ordination_test;

using shared.Model;

/// <summary>
/// Blackbox-tests for PN (Pro Necessitate – efter behov).
///
/// Metoder testet:
///   givDosis(dato)       → true hvis dato ∈ [startDen; slutDen], ellers false
///   samletDosis()        = dates.Count() * antalEnheder
///   doegnDosis()         = antalEnheder / dates.Count()  (NB: Infinity ved 0 givne doser)
///   getAntalGangeGivet() = dates.Count()
///
/// Ækvivalensklasser for givDosis:
///   EP1 (gyldig)       – dato er inden for [startDen; slutDen]
///   EP2 (ugyldig)      – dato er før startDen
///   EP3 (ugyldig)      – dato er efter slutDen
///   EP4 (BVA-grænse)   – dato = startDen (nedre grænse)
///   EP5 (BVA-grænse)   – dato = slutDen (øvre grænse)
///   EP6 (BVA-grænse)   – dato = startDen − 1 dag (én dag udenfor)
///   EP7 (BVA-grænse)   – dato = slutDen + 1 dag (én dag udenfor)
///
/// Ækvivalensklasser for doegnDosis:
///   EP8 (gyldig)   – dates.Count() &gt; 0
///   EP9 (ugyldig)  – dates.Count() = 0 → division med 0 (returnerer Infinity)
/// </summary>
[TestClass]
public class PNTest
{
    // PN: start 15-04-2026, slut 17-04-2026, 2 enheder pr. dosis
    private PN pn = null!;

    [TestInitialize]
    public void Setup()
    {
        Laegemiddel panodil = new Laegemiddel("Panodil", 1, 1.5, 2, "Styk");
        pn = new PN(new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), 2, panodil);
    }

    /// <summary>
    /// TC1 – givDosis inden for periode returnerer true (EP1).
    /// dates.Count = 1, samletDosis = 1 * 2 = 2, doegnDosis = 2 / 1 = 2.
    /// </summary>
    [TestMethod]
    public void TC1_GivDosis_IndenForPeriode_ReturnererTrue()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 16) };

        bool resultat = pn.givDosis(dato);

        Assert.IsTrue(resultat);
        Assert.AreEqual(1, pn.getAntalGangeGivet());
        Assert.AreEqual(2, pn.samletDosis(), 0.0001);
        Assert.AreEqual(2, pn.doegnDosis(), 0.0001);
    }

    /// <summary>
    /// TC2 – givDosis før startDen returnerer false (EP2).
    /// Dato registreres ikke i systemet.
    /// </summary>
    [TestMethod]
    public void TC2_GivDosis_FoerStartDen_ReturnererFalse()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 14) };

        bool resultat = pn.givDosis(dato);

        Assert.IsFalse(resultat);
        Assert.AreEqual(0, pn.getAntalGangeGivet());
    }

    /// <summary>
    /// TC3 – givDosis efter slutDen returnerer false (EP3).
    /// Dato registreres ikke i systemet.
    /// </summary>
    [TestMethod]
    public void TC3_GivDosis_EfterSlutDen_ReturnererFalse()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 18) };

        bool resultat = pn.givDosis(dato);

        Assert.IsFalse(resultat);
        Assert.AreEqual(0, pn.getAntalGangeGivet());
    }

    /// <summary>
    /// TC4 – BVA: givDosis på præcis startDen (nedre grænse, EP4).
    /// Grænseværdi: første gyldige dag skal accepteres.
    /// </summary>
    [TestMethod]
    public void TC4_BVA_GivDosis_PaaStartDen_ReturnererTrue()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 15) };

        bool resultat = pn.givDosis(dato);

        Assert.IsTrue(resultat);
        Assert.AreEqual(1, pn.getAntalGangeGivet());
    }

    /// <summary>
    /// TC5 – BVA: givDosis på præcis slutDen (øvre grænse, EP5).
    /// Grænseværdi: sidste gyldige dag skal accepteres.
    /// </summary>
    [TestMethod]
    public void TC5_BVA_GivDosis_PaaSlutDen_ReturnererTrue()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 17) };

        bool resultat = pn.givDosis(dato);

        Assert.IsTrue(resultat);
        Assert.AreEqual(1, pn.getAntalGangeGivet());
    }

    /// <summary>
    /// TC6 – BVA: givDosis én dag før startDen (EP6, EP3-grænse).
    /// Grænseværdi: én dag udenfor periode skal afvises.
    /// </summary>
    [TestMethod]
    public void TC6_BVA_GivDosis_EnDagFoerStart_ReturnererFalse()
    {
        Dato dato = new Dato { dato = new DateTime(2026, 4, 14) };

        bool resultat = pn.givDosis(dato);

        Assert.IsFalse(resultat);
        Assert.AreEqual(0, pn.getAntalGangeGivet());
    }

    /// <summary>
    /// TC7 – doegnDosis() uden givne doser → division med 0 (EP9).
    /// C# double-division med 0 kaster ikke exception, men returnerer Infinity.
    /// Afslører manglende validering i doegnDosis().
    /// </summary>
    [TestMethod]
    public void TC7_DoegnDosis_UdenGivneDosis_GiverInfinity()
    {
        // Ingen givDosis kaldt – dates er tom
        double result = pn.doegnDosis();

        Assert.IsTrue(double.IsInfinity(result) || double.IsNaN(result),
            "Ugyldig: doegnDosis() med 0 givne doser giver Infinity – validering mangler");
    }
}
