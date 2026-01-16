from flask import Flask, request, jsonify
from influxdb import InfluxDBClient
from flask_cors import CORS
import time

app = Flask(__name__)
CORS(app)

# Konfigurasi Database
# 'influx-db' adalah nama service di docker-compose
DB_HOST = 'influx-db'
DB_PORT = 8086
DB_NAME = 'bioskop_analytics'

# Fungsi koneksi dengan Retry Logic (Penting agar tidak error saat DB baru nyala)
def get_db_client():
    client = None
    for i in range(10):
        try:
            client = InfluxDBClient(host=DB_HOST, port=DB_PORT)
            # Cek apakah database sudah ada, kalau belum buat baru
            existing_dbs = client.get_list_database()
            if not any(db['name'] == DB_NAME for db in existing_dbs):
                client.create_database(DB_NAME)
            client.switch_database(DB_NAME)
            print("Connected to InfluxDB!")
            return client
        except Exception as e:
            print(f"Waiting for InfluxDB... ({e})")
            time.sleep(2)
    return None

# Inisialisasi client
db_client = None

@app.route('/')
def home():
    return jsonify({
        "service": "Analytics Service",
        "language": "Python",
        "database": "InfluxDB (Time Series)"
    })

# API 1: Catat Log (POST)
# Data masuk: { "movie": "Avengers", "action": "view_detail" }
@app.route('/log', methods=['POST'])
def log_activity():
    global db_client
    if not db_client: db_client = get_db_client()
    
    data = request.json
    movie = data.get('movie', 'Unknown')
    action = data.get('action', 'view')

    # Format data untuk InfluxDB
    json_body = [
        {
            "measurement": "user_activity",
            "tags": {
                "movie": movie,
                "action": action
            },
            "fields": {
                "value": 1  # Hitungan 1 klik
            }
        }
    ]

    try:
        db_client.write_points(json_body)
        return jsonify({"message": "Log saved", "data": json_body})
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# API 2: Lihat Statistik (GET)
# Menghitung berapa kali film dilihat
# API 2: Lihat Statistik (GET)
@app.route('/stats', methods=['GET'])
def get_stats():
    global db_client
    if not db_client: db_client = get_db_client()

    query = 'SELECT count("value") FROM "user_activity" GROUP BY "movie"'
    result = db_client.query(query)
    
    stats = []
    # PERBAIKAN DI SINI:
    # Kita harus mengambil 'tags' (nama movie) dari hasil query
    # result.items() mengembalikan pasangan (kunci, data)
    for key, points in result.items():
        # key[1] berisi tags, contoh: {'movie': 'Avengers'}
        tags = key[1] 
        for point in points:
            # Gabungkan tags (nama film) ke dalam data point
            point.update(tags)
            stats.append(point)
        
    return jsonify(stats)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)