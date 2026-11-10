var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Demo recommendation pipeline (Algorithm 1 only). DemoModelProvider loads/validates the
// pre-trained model once; DemoRecommendationService creates a fresh network per call.
builder.Services.AddSingleton(sp =>
    new Voluneta_Recommendation_Algorithm.DemoData.DemoModelProvider(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath));
builder.Services.AddSingleton<Voluneta_Recommendation_Algorithm.DemoData.DemoRecommendationService>();

// Demo allocation comparison (Algorithm 2 only). Reuses the Algorithm 1 scores above;
// no controllers/UI are wired up yet for this service.
builder.Services.AddSingleton<Voluneta_Recommendation_Algorithm.DemoData.DemoAllocationComparisonService>();

var app = builder.Build();

// Minimal startup validation: load the pre-trained model and confirm a sample prediction
// is finite and within [0, 1]. This does not retrain or modify the model file.
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        var modelProvider = new Voluneta_Recommendation_Algorithm.DemoData.DemoModelProvider(app.Environment.ContentRootPath);
        var network = modelProvider.CreateSessionNetwork();

        double[] sampleFeatures = [0.8, 0.6, 1.0, 0.5, 0.9, 0.7, 1.0, 0.4];
        double prediction = network.Forward(sampleFeatures);

        if (!double.IsFinite(prediction) || prediction < 0 || prediction > 1)
        {
            logger.LogError("Recommendation model validation failed: prediction {Prediction} is not finite in [0, 1].", prediction);
        }
        else
        {
            logger.LogInformation("Recommendation model loaded successfully. Sample prediction: {Prediction}.", prediction);
        }

        // Demonstrates Algorithm 1 (feature calculation + neural network scoring) independently,
        // using the demo volunteers/causes. No allocation priorities or capacity limits applied.
        var recommendationService = new Voluneta_Recommendation_Algorithm.DemoData.DemoRecommendationService(modelProvider);
        var rankedResults = recommendationService.GenerateRankedRecommendations();

        foreach (var result in rankedResults)
        {
            logger.LogInformation(
                "Volunteer {VolunteerId} - Cause {CauseId}: Rank {Rank}, Score {Score}.",
                result.VolunteerId, result.CauseId, result.Rank, result.Score);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Recommendation model failed to load.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
