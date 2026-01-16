using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// ================= CONNECTION STRING =================
string masterConnection = "Server=mssql-db,1433;Database=master;User Id=sa;Password=Mypassw0rd!;TrustServerCertificate=True;";
string bookingConnection = "Server=mssql-db,1433;Database=BookingDB;User Id=sa;Password=Mypassw0rd!;TrustServerCertificate=True;";

// ================= BACKGROUND INIT DATABASE =================
_ = Task.Run(async () => {
    Console.WriteLine("🚀 Inisialisasi Database dimulai...");
    for (int i = 0; i < 20; i++) 
    {
        try 
        {
            using (var conn = new SqlConnection(masterConnection)) {
                await conn.OpenAsync();
                await new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BookingDB') CREATE DATABASE BookingDB;", conn).ExecuteNonQueryAsync();
            }

            using (var conn = new SqlConnection(bookingConnection)) {
                await conn.OpenAsync();
                var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Seats' AND xtype='U')
                BEGIN
                    CREATE TABLE Seats (
                        Movie VARCHAR(100),
                        SeatNumber VARCHAR(10),
                        IsBooked BIT DEFAULT 0,
                        PRIMARY KEY (Movie, SeatNumber)
                    );
                END";
                await new SqlCommand(sql, conn).ExecuteNonQueryAsync();
            }
            Console.WriteLine("✅ SQL Server: Database & Table Ready!");
            break;
        } 
        catch (Exception ex) {
            Console.WriteLine($"⏳ Menunggu SQL Server... ({i+1}/20) - Error: {ex.Message}");
            await Task.Delay(5000);
        }
    }
});

// ================= ROUTES =================

app.MapGet("/", () => new { status = "Online", message = "C# Booking Service is Running with 50 Seats Logic" });

// 🔹 GET SEATS (LOGIKA AUTO-GENERATE 50 KURSI)
app.MapGet("/seats/{movie}", async (string movie) =>
{
    var seats = new List<object>();
    var movieTitle = movie.Trim();

    try {
        using var conn = new SqlConnection(bookingConnection);
        await conn.OpenAsync();

        // Cek apakah data kursi untuk film ini sudah ada
        var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Seats WHERE LTRIM(RTRIM(Movie)) = @movie", conn);
        checkCmd.Parameters.AddWithValue("@movie", movieTitle);
        int count = (int)await checkCmd.ExecuteScalarAsync();

        // JIKA KOSONG, KITA BUAT 50 KURSI (5 BARIS [A-E] x 10 KOLOM [1-10])
        if (count == 0)
        {
            Console.WriteLine($"🆕 Film Baru: {movieTitle}. Menghasilkan 50 kursi (A1-E10)...");
            
            using (var insertCmd = new SqlCommand())
            {
                insertCmd.Connection = conn;
                var valuesList = new List<string>();
                
                char[] rows = { 'A', 'B', 'C', 'D', 'E' };
                for (int r = 0; r < rows.Length; r++)
                {
                    for (int c = 1; c <= 10; c++)
                    {
                        string seatName = $"{rows[r]}{c}";
                        int idx = (r * 10) + (c - 1); // Index unik untuk parameter
                        
                        valuesList.Add($"(@m, @s{idx}, 0)");
                        insertCmd.Parameters.AddWithValue($"@s{idx}", seatName);
                    }
                }

                insertCmd.CommandText = $"INSERT INTO Seats (Movie, SeatNumber, IsBooked) VALUES {string.Join(",", valuesList)}";
                insertCmd.Parameters.AddWithValue("@m", movieTitle);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        // Ambil data kursi (baik yang baru dibuat atau yang sudah ada)
        var cmd = new SqlCommand("SELECT SeatNumber, IsBooked FROM Seats WHERE LTRIM(RTRIM(Movie)) = @movie ORDER BY SeatNumber ASC", conn);
        cmd.Parameters.AddWithValue("@movie", movieTitle);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            seats.Add(new { 
                id = reader.GetString(0), 
                isBooked = reader.GetBoolean(1) 
            });
        }
        
        return Results.Ok(seats);
    } 
    catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// 🔹 POST BOOKING
app.MapPost("/api/booking", async (BookingRequest req) =>
{
    try {
        using var conn = new SqlConnection(bookingConnection);
        await conn.OpenAsync();

        var check = new SqlCommand("SELECT IsBooked FROM Seats WHERE TRIM(Movie)=@movie AND SeatNumber=@seat", conn);
        check.Parameters.AddWithValue("@movie", req.movie.Trim());
        check.Parameters.AddWithValue("@seat", req.seat);
        var status = await check.ExecuteScalarAsync();

        if (status == null) return Results.NotFound("Kursi tidak ditemukan");
        if ((bool)status) return Results.BadRequest("Kursi sudah dipesan");

        var book = new SqlCommand("UPDATE Seats SET IsBooked=1 WHERE TRIM(Movie)=@movie AND SeatNumber=@seat", conn);
        book.Parameters.AddWithValue("@movie", req.movie.Trim());
        book.Parameters.AddWithValue("@seat", req.seat);
        await book.ExecuteNonQueryAsync();

        return Results.Ok(new { message = $"Berhasil memesan kursi {req.seat}" });
    } 
    catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

app.Run();

public record BookingRequest(string movie, string seat, string user);