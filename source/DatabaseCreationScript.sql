CREATE TABLE Users(
	id_user INTEGER PRIMARY KEY,
	first_name VARCHAR(50) NOT NULL,
	last_name VARCHAR(50) NOT NULL,
	email VARCHAR(100) NOT NULL UNIQUE,
	salt VARCHAR(32) NOT NULL,
	hash VARCHAR(128) NOT NULL,
	two_fa_key VARCHAR(255),
	two_fa_uri VARCHAR(255)
);

CREATE TABLE RefreshTokens (
    id_refreshToken INTEGER PRIMARY KEY IDENTITY(1,1),
    token VARCHAR(255) NOT NULL,
    expiry_date DATETIME NOT NULL,
    id_user INTEGER NOT NULL,
    FOREIGN KEY (id_user) REFERENCES Users(id_user) ON DELETE CASCADE
);