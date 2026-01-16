CREATE DATABASE IF NOT EXISTS bioskop_catalog;
USE bioskop_catalog;

CREATE TABLE IF NOT EXISTS movies (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    genre VARCHAR(100),
    price DECIMAL(10, 2),
    schedule VARCHAR(100)
);

INSERT INTO movies (title, genre, price, schedule) VALUES
('Avengers: Secret Wars', 'Action', 50000.00, '19:00'),
('Spider-Man: Beyond Spider-Verse', 'Animation', 45000.00, '16:30'),
('Pengabdi Setan 3', 'Horror', 40000.00, '21:00');