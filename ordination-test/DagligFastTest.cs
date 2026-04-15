namespace ordination_test;

using shared.Model;

/// <summary>
/// Blackbox-tests for DagligFast (klassemodel).
///
/// Metoder testet:
///   doegnDosis()  = morgenAntal + middagAntal + aftenAntal + natAntal
///   samletDosis() = antalDage() * doegnDosis()
///   getDoser()    = array af 4 Dosis-objekter med faste tidspunkter
///   getType()     = "DagligFast"
///
/// Ækvivalensklasser:
///   EP1 (gyldig)       – startDen &lt; slutDen
///   EP2 (gyldig, BVA)  – startDen = slutDen
///   EP3 (ugyldig)      – startDen &gt; slutDen
///   EP4 (gyldig)       – laegemiddel eksisterer (ikke null)
///   EP6 (gyldig)       – alle doser &gt; 0
///   EP7 (gyldig, BVA)  – alle doser = 0 (minimumsgrænse)
///   EP8 (ugyldig)      – dosisAntal &lt; 0
/// </summary>
[TestClass]
public class DagligFastTest
{
    private Laegemiddel panodil = null!;

    [TestInitialize]
    public void Setup()
    {
        panodil = new Laegemiddel("Panodil", 1, 1.5, 2, "Styk");
    }

    /// <summary>
    /// TC1 – Gyldig DagligFast oprettes (EP1, EP4, EP6).
    /// 3-dages periode, alle doser = 1.
    /// doegnDosis = 1+1+1+1 = 4, samletDosis = 3 * 4 = 12.
    /// Tidspunkter: morgen 06:00, middag 12:00, aften 18:00, nat 23:59.
    /// </summary>
    [TestMethod]
    public void TC1_GyldigDagligFast_KorrekteDosisBeregninger()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil,
            morgenAntal: 1, middagAntal: 1, aftenAntal: 1, natAntal: 1);

        Assert.AreEqual(3,            df.antalDage());
        Assert.AreEqual(4,            df.doegnDosis());
        Assert.AreEqual(12,           df.samletDosis());
        Assert.AreEqual("DagligFast", df.getType());

        // Tidspunkter sættes automatisk af konstruktøren
        Dosis[] doser = df.getDoser();
        Assert.AreEqual(4, doser.Length);
        Assert.AreEqual(new TimeSpan(6,  0,  0), doser[0].tid.TimeOfDay); // morgen 06:00
        Assert.AreEqual(new TimeSpan(12, 0,  0), doser[1].tid.TimeOfDay); // middag 12:00
        Assert.AreEqual(new TimeSpan(18, 0,  0), doser[2].tid.TimeOfDay); // aften  18:00
        Assert.AreEqual(new TimeSpan(23, 59, 0), doser[3].tid.TimeOfDay); // nat    23:59
    }

    /// <summary>
    /// TC2 – startDen &gt; slutDen → ugyldig tilstand (EP3).
    /// Afslører manglende validering: antalDage() bliver negativ,
    /// og samletDosis() returnerer en negativ/ugyldig værdi.
    /// </summary>
    [TestMethod]
    public void TC2_StartDenStoerreEndSlutDen_GiverUgyldigTilstand()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 30), new DateTime(2026, 4, 17), panodil,
            morgenAntal: 1, middagAntal: 1, aftenAntal: 1, natAntal: 1);

        Assert.IsTrue(df.antalDage() <= 0,
            "Ugyldig: startDen > slutDen skal give antalDage <= 0");
    }

    /// <summary>
    /// TC3 – BVA: startDen = slutDen → præcis 1 dag (EP2).
    /// samletDosis = 1 * doegnDosis = 5.
    /// </summary>
    [TestMethod]
    public void TC3_BVA_StartDenLigSlutDen_EnDag()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 15), panodil,
            morgenAntal: 2, middagAntal: 2, aftenAntal: 1, natAntal: 0);

        Assert.AreEqual(1, df.antalDage());
        Assert.AreEqual(5, df.doegnDosis());
        Assert.AreEqual(5, df.samletDosis());
    }

    /// <summary>
    /// TC4 – BVA: alle dosisantal = 0 (EP7, minimumsgrænse).
    /// doegnDosis = 0, samletDosis = 3 * 0 = 0.
    /// </summary>
    [TestMethod]
    public void TC4_BVA_AlleDoserNul_GiverNulDosis()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil,
            morgenAntal: 0, middagAntal: 0, aftenAntal: 0, natAntal: 0);

        Assert.AreEqual(3, df.antalDage());
        Assert.AreEqual(0, df.doegnDosis());
        Assert.AreEqual(0, df.samletDosis());
    }

    /// <summary>
    /// TC5 – Negativt dosisantal → ugyldig tilstand (EP8).
    /// Afslører manglende validering: doegnDosis() returnerer en negativ sum.
    /// </summary>
    [TestMethod]
    public void TC5_NegativtDosisAntal_GiverUgyldigDoegnDosis()
    {
        DagligFast df = new DagligFast(
            new DateTime(2026, 4, 15), new DateTime(2026, 4, 17), panodil,
            morgenAntal: -1, middagAntal: 1, aftenAntal: 1, natAntal: 1);

        Assert.IsTrue(df.doegnDosis() < 3,
            "Ugyldig: negativt dosisantal påvirker doegnDosis – validering mangler");
    }
}
