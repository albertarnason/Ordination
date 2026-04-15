namespace ordination_test;

using shared.Model;
using static shared.Util;

/// <summary>
/// Blackbox-tests for DagligSkæv (klassemodel).
///
/// Metoder testet:
///   doegnDosis()       = sum af alle doser i listen
///   samletDosis()      = antalDage() * doegnDosis()
///   opretDosis(tid, antal) = tilføjer ny Dosis til listen
///
/// Ækvivalensklasser:
///   EP1 (gyldig)       – startDen &lt; slutDen
///   EP2 (gyldig, BVA)  – startDen = slutDen
///   EP3 (ugyldig)      – startDen &gt; slutDen
///   EP4 (gyldig)       – laegemiddel eksisterer (ikke null)
///   EP6 (gyldig)       – dosisliste har 1+ poster med antal &gt; 0
///   EP7 (gyldig, BVA)  – tom dosisliste (minimumsgrænse)
///   EP8 (ugyldig)      – dosis med antal &lt; 0
/// </summary>
[TestClass]
public class DagligSkaevTest
{
    private Laegemiddel panodil = null!;

    [TestInitialize]
    public void Setup()
    {
        panodil = new Laegemiddel("Panodil", 1, 1.5, 2, "Styk");
    }

    /// <summary>
    /// TC1 – Gyldig DagligSkæv med flere doser (EP1, EP4, EP6).
    /// 3-dages periode, doser: 0.5 + 1 + 2 = 3.5 pr. døgn.
    /// samletDosis = 3 * 3.5 = 10.5.
    /// </summary>
    [TestMethod]
    public void TC1_GyldigDagligSkaev_FlereDoser_KorrektBeregning()
    {
        Dosis[] doser = {
            new Dosis(CreateTimeOnly(8,  0, 0), 0.5),
            new Dosis(CreateTimeOnly(12, 0, 0), 1.0),
            new Dosis(CreateTimeOnly(20, 0, 0), 2.0)
        };

        DagligSkæv ds = new DagligSkæv(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil, doser);

        Assert.AreEqual(3,           ds.antalDage());
        Assert.AreEqual(3.5,         ds.doegnDosis(), 0.0001);
        Assert.AreEqual(10.5,        ds.samletDosis(), 0.0001);
        Assert.AreEqual("DagligSkæv", ds.getType());
    }

    /// <summary>
    /// TC2 – startDen &gt; slutDen → ugyldig tilstand (EP3).
    /// Afslører manglende validering: antalDage() bliver negativ.
    /// </summary>
    [TestMethod]
    public void TC2_StartDenStoerreEndSlutDen_GiverUgyldigTilstand()
    {
        Dosis[] doser = { new Dosis(CreateTimeOnly(8, 0, 0), 1.0) };

        DagligSkæv ds = new DagligSkæv(
            new DateTime(2026, 4, 30), new DateTime(2026, 4, 17), panodil, doser);

        Assert.IsTrue(ds.antalDage() <= 0,
            "Ugyldig: startDen > slutDen skal give antalDage <= 0");
    }

    /// <summary>
    /// TC3 – BVA: startDen = slutDen → præcis 1 dag (EP2).
    /// samletDosis = 1 * doegnDosis = 2.
    /// </summary>
    [TestMethod]
    public void TC3_BVA_StartDenLigSlutDen_EnDag()
    {
        Dosis[] doser = {
            new Dosis(CreateTimeOnly(8,  0, 0), 1.0),
            new Dosis(CreateTimeOnly(20, 0, 0), 1.0)
        };

        DagligSkæv ds = new DagligSkæv(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 15), panodil, doser);

        Assert.AreEqual(1, ds.antalDage());
        Assert.AreEqual(2, ds.doegnDosis(), 0.0001);
        Assert.AreEqual(2, ds.samletDosis(), 0.0001);
    }

    /// <summary>
    /// TC4 – BVA: tom dosisliste (EP7, minimumsgrænse).
    /// doegnDosis = 0, samletDosis = 3 * 0 = 0.
    /// </summary>
    [TestMethod]
    public void TC4_BVA_TomDosisListe_GiverNulDosis()
    {
        DagligSkæv ds = new DagligSkæv(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil);

        Assert.AreEqual(3, ds.antalDage());
        Assert.AreEqual(0, ds.doegnDosis(), 0.0001);
        Assert.AreEqual(0, ds.samletDosis(), 0.0001);
    }

    /// <summary>
    /// TC5 – Dosis med negativt antal → ugyldig tilstand (EP8).
    /// Afslører manglende validering: doegnDosis() returnerer negativ sum.
    /// </summary>
    [TestMethod]
    public void TC5_NegativtDosisAntal_GiverUgyldigDoegnDosis()
    {
        Dosis[] doser = { new Dosis(CreateTimeOnly(8, 0, 0), -1.0) };

        DagligSkæv ds = new DagligSkæv(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil, doser);

        Assert.IsTrue(ds.doegnDosis() < 0,
            "Ugyldig: negativ dosis giver negativ doegnDosis – validering mangler");
    }
}
