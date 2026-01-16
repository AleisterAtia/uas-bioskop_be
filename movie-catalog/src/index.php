<?php
header("Content-Type: application/json");
header("Access-Control-Allow-Origin: *"); // Penting untuk Flutter/Frontend nanti
header("Access-Control-Allow-Methods: GET, POST, PUT, DELETE");
header("Access-Control-Allow-Headers: Content-Type");

$servername = "mysql-db";
$username = "root";
$password = "root";
$dbname = "bioskop_catalog";

// 1. Koneksi Database dengan Retry Logic
$conn = null;
for ($i = 0; $i < 10; $i++) {
    $conn = new mysqli($servername, $username, $password, $dbname);
    if ($conn->connect_error) {
        sleep(2);
        continue;
    }
    break;
}

if ($conn->connect_error) {
    die(json_encode(["error" => "Connection failed: " . $conn->connect_error]));
}

// 2. Cek Metode HTTP (GET, POST, PUT, DELETE)
$method = $_SERVER['REQUEST_METHOD'];
$input = json_decode(file_get_contents('php://input'), true);

switch ($method) {
    case 'GET':
        handleGet($conn);
        break;
    case 'POST':
        handlePost($conn, $input);
        break;
    case 'PUT':
        handlePut($conn, $input);
        break;
    case 'DELETE':
        handleDelete($conn);
        break;
    default:
        echo json_encode(["message" => "Method not supported"]);
        break;
}

// --- FUNGSI-FUNGSI CRUD ---

function handleGet($conn) {
    $sql = "SELECT * FROM movies";
    $result = $conn->query($sql);
    
    $movies = [];
    if ($result->num_rows > 0) {
        while($row = $result->fetch_assoc()) {
            $movies[] = $row;
        }
    }
    echo json_encode($movies);
}

function handlePost($conn, $input) {
    $title = $input['title'];
    $genre = $input['genre'];
    $price = $input['price'];
    $schedule = $input['schedule'];

    $stmt = $conn->prepare("INSERT INTO movies (title, genre, price, schedule) VALUES (?, ?, ?, ?)");
    $stmt->bind_param("ssds", $title, $genre, $price, $schedule);

    if ($stmt->execute()) {
        echo json_encode(["message" => "Film berhasil ditambahkan", "id" => $stmt->insert_id]);
    } else {
        http_response_code(500);
        echo json_encode(["error" => "Gagal menambah film"]);
    }
    $stmt->close();
}

function handlePut($conn, $input) {
    // Kita butuh ID untuk update (dikirim via JSON body)
    if (!isset($input['id'])) {
        http_response_code(400);
        echo json_encode(["error" => "ID film diperlukan untuk update"]);
        return;
    }

    $id = $input['id'];
    $title = $input['title'];
    $genre = $input['genre'];
    $price = $input['price'];
    $schedule = $input['schedule'];

    $stmt = $conn->prepare("UPDATE movies SET title=?, genre=?, price=?, schedule=? WHERE id=?");
    $stmt->bind_param("ssdsi", $title, $genre, $price, $schedule, $id);

    if ($stmt->execute()) {
        echo json_encode(["message" => "Film berhasil diupdate"]);
    } else {
        http_response_code(500);
        echo json_encode(["error" => "Gagal update film"]);
    }
    $stmt->close();
}

function handleDelete($conn) {
    // ID diambil dari URL query string (contoh: ?id=1)
    if (!isset($_GET['id'])) {
        http_response_code(400);
        echo json_encode(["error" => "ID film diperlukan di URL"]);
        return;
    }

    $id = $_GET['id'];
    $stmt = $conn->prepare("DELETE FROM movies WHERE id=?");
    $stmt->bind_param("i", $id);

    if ($stmt->execute()) {
        echo json_encode(["message" => "Film berhasil dihapus"]);
    } else {
        http_response_code(500);
        echo json_encode(["error" => "Gagal hapus film"]);
    }
    $stmt->close();
}

$conn->close();
?>