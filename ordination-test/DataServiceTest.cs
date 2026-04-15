namespace ordination_test;

using Microsoft.EntityFrameworkCore;
using Service;
using Data;
using shared.Model;

/// <summary>
/// Blackbox-tests for DataService (API-lag).
///
/// Metoder testet:
///   OpretDagligFast(patientId, lmId, morgen, middag, aften, nat, start, slut)
///   AnvendOrdination(id, dato)
///   GetAnbefaletDosisPerDøgn(patientId, lmId)
///
/// Ækvivalensklasser:
///   EP1 (gyldig)       – startDen &lt; slutDen, patient og lm eksisterer
///   EP3 (ugyldig)      – startDen &gt; slutDen
///   EP5 (ugyldig)      – patient eksisterer ikke (null-reference)
///
/// Ækvivalensklasser for AnvendOrdination:
///   EP_A1 – id er PN og dato er inden for periode → "anvendt"
///   EP_A2 – id er PN og dato er uden for periode → "dato uden for periode"
///   EP_A3 – id er ikke PN (f.eks. DagligFast) → "ikke fundet"
///
/// Ækvivalensklasser for GetAnbefaletDosisPerDøgn (BVA på vægt):
///   EP_V1 – vaegt &lt; 25 kg (let)           → enhedPrKgPrDoegnLet * vaegt
///   EP_V2 – 25 ≤ vaegt &lt; 120 kg (normal)  → enhedPrKgPrDoegnNormal * vaegt
///   EP_V3 – vaegt ≥ 120 kg (tung)          → enhedPrKgPrDoegnTung * vaegt
///   BVA   – vaegt = 25  (grænse Let/Normal)
///   BVA   – vaegt = 120 (grænse Normal/Tung)
/// </summary>
[TestClass]
public class DataServiceTest
{
    private DataService service = null!;
    private OrdinationContext context = null!;

    [TestInitialize]
    public void SetupBeforeEachTest()
    {
        // Unik databasenavn per test for at undgå cross-test forurening
        var optionsBuilder = new DbContextOptionsBuilder<OrdinationContext>();
        optionsBuilder.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
        context = new OrdinationContext(optionsBuilder.Options);
        service = new DataService(context);
        service.SeedData();
    }

    // ────────────────────────────────────────────────────────────────────────
    // OpretDagligFast
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TC1 – OpretDagligFast med gyldige inputs (EP1).
    /// DagligFast oprettes og knyttes til patienten.
    /// </summary>
    [TestMethod]
    public void TC1_OpretDagligFast_GyldigeInputs_OprettesKorrekt()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm  = service.GetLaegemidler().First();
        int antalFoer   = service.GetDagligFaste().Count;

        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            1, 1, 1, 0, new DateTime(2026, 4, 15), new DateTime(2026, 4, 17));

        Assert.AreEqual(antalFoer + 1, service.GetDagligFaste().Count);
    }

    /// <summary>
    /// TC2 – OpretDagligFast: patient eksisterer ikke (EP5).
    /// db.Patienter.Find() returnerer null → NullReferenceException.
    /// Afslører at service ikke validerer patientId.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(NullReferenceException))]
    public void TC2_OpretDagligFast_PatientEksistererIkke_KasterException()
    {
        Laegemiddel lm = service.GetLaegemidler().First();

        service.OpretDagligFast(99999, lm.LaegemiddelId,
            1, 1, 1, 0, new DateTime(2026, 4, 15), new DateTime(2026, 4, 17));
    }

    /// <summary>
    /// TC3 – OpretDagligFast: startDato &gt; slutDato (EP3).
    /// Afslører manglende validering: objektet oprettes men antalDage er negativt.
    /// </summary>
    [TestMethod]
    public void TC3_OpretDagligFast_StartStoerreEndSlut_GiverUgyldigOrdination()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm  = service.GetLaegemidler().First();

        DagligFast df = service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            1, 1, 1, 0, new DateTime(2026, 4, 30), new DateTime(2026, 4, 17));

        Assert.IsTrue(df.antalDage() <= 0,
            "Ugyldig: startDato > slutDato giver antalDage <= 0 – validering mangler");
    }

    // ────────────────────────────────────────────────────────────────────────
    // AnvendOrdination
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TC4 – AnvendOrdination: dato inden for PN-periode (EP_A1).
    /// Seeded PN: 01-01-2021 til 12-01-2021.
    /// Forventet: "anvendt".
    /// </summary>
    [TestMethod]
    public void TC4_AnvendOrdination_DatoIndenForPNPeriode_Anvendt()
    {
        PN pn = service.GetPNs().First();
        Dato dato = new Dato { dato = new DateTime(2021, 1, 5) };

        string resultat = service.AnvendOrdination(pn.OrdinationId, dato);

        Assert.AreEqual("anvendt", resultat);
    }

    /// <summary>
    /// TC5 – AnvendOrdination: dato uden for PN-periode (EP_A2).
    /// Seeded PN: 01-01-2021 til 12-01-2021.
    /// Forventet: "dato uden for periode".
    /// </summary>
    [TestMethod]
    public void TC5_AnvendOrdination_DatoUdenForPNPeriode_UdenForPeriode()
    {
        PN pn = service.GetPNs().First();
        Dato dato = new Dato { dato = new DateTime(2021, 2, 1) };

        string resultat = service.AnvendOrdination(pn.OrdinationId, dato);

        Assert.AreEqual("dato uden for periode", resultat);
    }

    /// <summary>
    /// TC6 – AnvendOrdination: id er en DagligFast, ikke en PN (EP_A3).
    /// Forventet: "ikke fundet".
    /// </summary>
    [TestMethod]
    public void TC6_AnvendOrdination_IkkePN_IkkeFundet()
    {
        DagligFast df = service.GetDagligFaste().First();
        Dato dato = new Dato { dato = new DateTime(2021, 1, 11) };

        string resultat = service.AnvendOrdination(df.OrdinationId, dato);

        Assert.AreEqual("ikke fundet", resultat);
    }

    // ────────────────────────────────────────────────────────────────────────
    // GetAnbefaletDosisPerDøgn  (Paracetamol: Let=1, Normal=1.5, Tung=2 ml/kg)
    // ────────────────────────────────────────────────────────────────────────

    private (int patientId, int lmId) OpretPatientOgHentParacetamol(double vaegt)
    {
        Patient p = new Patient("000000-0000", "Test Patient", vaegt);
        context.Patienter.Add(p);
        context.SaveChanges();

        int lmId = service.GetLaegemidler()
            .First(lm => lm.navn == "Paracetamol").LaegemiddelId;

        return (p.PatientId, lmId);
    }

    /// <summary>
    /// TC7 – GetAnbefaletDosisPerDøgn: let patient, vaegt = 20 kg (EP_V1, vaegt &lt; 25).
    /// Paracetamol enhedPrKgPrDoegnLet = 1.0 → 1.0 * 20 = 20.0.
    /// </summary>
    [TestMethod]
    public void TC7_AnbefaletDosis_LetPatient_Vaegt20()
    {
        var (patientId, lmId) = OpretPatientOgHentParacetamol(vaegt: 20);

        double resultat = service.GetAnbefaletDosisPerDøgn(patientId, lmId);

        Assert.AreEqual(20.0, resultat, 0.0001);
    }

    /// <summary>
    /// TC8 – BVA: vaegt = 25 kg (grænse Let/Normal, EP_V2).
    /// Paracetamol enhedPrKgPrDoegnNormal = 1.5 → 1.5 * 25 = 37.5.
    /// </summary>
    [TestMethod]
    public void TC8_AnbefaletDosis_BVA_Vaegt25_GraenseLetNormal()
    {
        var (patientId, lmId) = OpretPatientOgHentParacetamol(vaegt: 25);

        double resultat = service.GetAnbefaletDosisPerDøgn(patientId, lmId);

        Assert.AreEqual(37.5, resultat, 0.0001);
    }

    /// <summary>
    /// TC9 – GetAnbefaletDosisPerDøgn: tung patient, vaegt = 130 kg (EP_V3, vaegt ≥ 120).
    /// Paracetamol enhedPrKgPrDoegnTung = 2.0 → 2.0 * 130 = 260.0.
    /// </summary>
    [TestMethod]
    public void TC9_AnbefaletDosis_TungPatient_Vaegt130()
    {
        var (patientId, lmId) = OpretPatientOgHentParacetamol(vaegt: 130);

        double resultat = service.GetAnbefaletDosisPerDøgn(patientId, lmId);

        Assert.AreEqual(260.0, resultat, 0.0001);
    }

    /// <summary>
    /// TC10 – BVA: vaegt = 120 kg (grænse Normal/Tung, EP_V3).
    /// Paracetamol enhedPrKgPrDoegnTung = 2.0 → 2.0 * 120 = 240.0.
    /// </summary>
    [TestMethod]
    public void TC10_AnbefaletDosis_BVA_Vaegt120_GraenseNormalTung()
    {
        var (patientId, lmId) = OpretPatientOgHentParacetamol(vaegt: 120);

        double resultat = service.GetAnbefaletDosisPerDøgn(patientId, lmId);

        Assert.AreEqual(240.0, resultat, 0.0001);
    }
}
