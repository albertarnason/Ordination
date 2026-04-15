namespace ordination_test;

using shared.Model;

/// <summary>
/// Blackbox-tests for Ordination (abstrakt baseklasse).
/// Testes via DagligFast, da Ordination er abstrakt.
///
/// Metode: antalDage() = (slutDen - startDen).Days + 1
///
/// Ækvivalensklasser:
///   EP1 (gyldig)         – startDen &lt; slutDen
///   EP2 (gyldig, BVA)    – startDen = slutDen (minimumsgrænse: 1 dag)
///   EP3 (ugyldig)        – startDen &gt; slutDen
/// </summary>
[TestClass]
public class OrdinationTest
{
    private Laegemiddel lm = null!;

    [TestInitialize]
    public void Setup()
    {
        lm = new Laegemiddel("Panodil", 1, 1.5, 2, "Styk");
    }

    /// <summary>
    /// TC1 – Korrekt antal dage for gyldig periode (EP1).
    /// startDen 15-04-2026, slutDen 17-04-2026 → 3 dage (15., 16., 17.).
    /// </summary>
    [TestMethod]
    public void TC1_AntalDage_GyldigPeriode_ReturnererKorrektAntal()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), lm, 1, 1, 1, 1);

        Assert.AreEqual(3, df.antalDage());
    }

    /// <summary>
    /// TC2 – BVA: startDen = slutDen → præcis 1 dag (EP2, minimumsgrænse).
    /// </summary>
    [TestMethod]
    public void TC2_AntalDage_BVA_SammeDag_ReturnererEn()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 15), lm, 1, 1, 1, 1);

        Assert.AreEqual(1, df.antalDage());
    }

    /// <summary>
    /// TC3 – BVA: slutDen = startDen + 1 dag → 2 dage.
    /// </summary>
    [TestMethod]
    public void TC3_AntalDage_BVA_EnDagForskel_ReturnererTo()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 16), lm, 1, 1, 1, 1);

        Assert.AreEqual(2, df.antalDage());
    }

    /// <summary>
    /// TC4 – Ugyldig: startDen &gt; slutDen → antalDage er negativ eller 0 (EP3).
    /// Afslører manglende validering: systemet bør afvise dette som fejl.
    /// </summary>
    [TestMethod]
    public void TC4_AntalDage_UgyldigStartStoerreEndSlut_GiverNegativt()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 17), new DateTime(2026, 4, 15), lm, 1, 1, 1, 1);

        Assert.IsTrue(df.antalDage() <= 0,
            "Ugyldig tilstand: startDen > slutDen skal give antalDage <= 0");
    }

    /// <summary>
    /// TC5 – BVA-grænse for EP3: startDen = slutDen + 1 dag → antalDage = 0.
    /// Grænseværdi præcis ved overgangen fra gyldig til ugyldig.
    /// </summary>
    [TestMethod]
    public void TC5_AntalDage_BVA_StartEnDagEfterSlut_GiverNul()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 16), new DateTime(2026, 4, 15), lm, 1, 1, 1, 1);

        Assert.AreEqual(0, df.antalDage());
    }
}
