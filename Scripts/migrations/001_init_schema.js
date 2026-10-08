/**
 * Migración 001 — Esquema inicial (users + matches)
 * Idempotente: se puede ejecutar múltiples veces sin problema.
 *
 * Uso:
 *   mongosh "mongodb://localhost:27017/GameDB" Scripts/migrations/001_init_schema.js
 *   docker exec -i game-mongodb mongosh "mongodb://localhost:27017/GameDB" < Scripts/migrations/001_init_schema.js
 */

const DB_NAME = "GameDB";
const db = db.getSiblingDB(DB_NAME);

print(`\n [001] Migración de esquema sobre '${DB_NAME}'`);
print("──────────────────────────────────────────────");

/* ============================================================
 * FASE 1 — Migración de datos existentes (legacy → nuevo esquema)
 * ============================================================ */
const colls = db.getCollectionNames();

// 1.1 Renombrar colección "Gameplays" → "matches"
if (colls.includes("Gameplays") && !colls.includes("matches")) {
    db.Gameplays.renameCollection("matches");
    print(" Colección 'Gameplays' renombrada a 'matches'");
}

// 1.2 Renombrar colección "Users" → "users"
if (colls.includes("Users") && !colls.includes("users")) {
    db.Users.renameCollection("users");
    print(" Colección 'Users' renombrada a 'users'");
}

// 1.3 Renombrar campos en 'users' (si quedan del esquema viejo)
if (db.getCollectionNames().includes("users")) {
    const r1 = db.users.updateMany(
        { name:     { $exists: true } },
        { $rename: { name: "username" } }
    );
    if (r1.modifiedCount) print(` users: 'name' → 'username' (${r1.modifiedCount} docs)`);

    const r2 = db.users.updateMany(
        { password: { $exists: true } },
        { $rename: { password: "password_hash" } }
    );
    if (r2.modifiedCount) print(` users: 'password' → 'password_hash' (${r2.modifiedCount} docs)`);

    const r3 = db.users.updateMany(
        { registerDate: { $exists: true } },
        { $rename: { registerDate: "createdDate" } }
    );
    if (r3.modifiedCount) print(` users: 'registerDate' → 'createdDate' (${r3.modifiedCount} docs)`);
}

// 1.4 Renombrar campos en 'matches' + añadir 'status'
if (db.getCollectionNames().includes("matches")) {
    const r4 = db.matches.updateMany(
        { currentPositions: { $exists: true } },
        { $rename: { currentPositions: "board_state" } }
    );
    if (r4.modifiedCount) print(` matches: 'currentPositions' → 'board_state' (${r4.modifiedCount} docs)`);

    const r5 = db.matches.updateMany(
        { hostPlayer:  { $exists: true } },
        { $rename: { hostPlayer: "player1" } }
    );
    if (r5.modifiedCount) print(` matches: 'hostPlayer' → 'player1' (${r5.modifiedCount} docs)`);

    const r6 = db.matches.updateMany(
        { guestPlayer: { $exists: true } },
        { $rename: { guestPlayer: "player2" } }
    );
    if (r6.modifiedCount) print(` matches: 'guestPlayer' → 'player2' (${r6.modifiedCount} docs)`);

    // Derivar 'status' a partir de 'matchResult'
    const r7 = db.matches.updateMany(
        { status: { $exists: false }, matchResult: { $ne: null } },
        { $set: { status: "finished" } }
    );
    if (r7.modifiedCount) print(` matches: 'status'='finished' derivado (${r7.modifiedCount} docs)`);

    const r8 = db.matches.updateMany(
        { status: { $exists: false } },
        { $set: { status: "in_progress" } }
    );
    if (r8.modifiedCount) print(` matches: 'status'='in_progress' por defecto (${r8.modifiedCount} docs)`);
}

/* ============================================================
 * FASE 2 — Validators $jsonSchema
 * ============================================================ */

const userValidator = {
    $jsonSchema: {
        bsonType: "object",
        required: ["username", "email", "password_hash", "createdDate"],
        properties: {
            _id:           { bsonType: "objectId" },
            username:      { bsonType: "string", minLength: 3, maxLength: 50 },
            email:         { bsonType: "string" },
            password_hash: { bsonType: "string", minLength: 20, description: "Hash BCrypt" },
            createdDate:   { bsonType: "date" }
        }
    }
};

const matchValidator = {
    $jsonSchema: {
        bsonType: "object",
        required: ["player1", "player2", "board_state", "status", "createdDate", "updatedDate"],
        properties: {
            _id:         { bsonType: "objectId" },
            player1:     { bsonType: ["objectId", "string"] },
            player2:     { bsonType: ["objectId", "string", "null"] },
            board_state: { bsonType: "string" },
            status:      { enum: ["in_progress", "finished", "abandoned"] },
            playerTurn:  { bsonType: ["objectId", "string"] },
            matchResult: { bsonType: ["objectId", "string", "null"] },
            createdDate: { bsonType: "date" },
            updatedDate: { bsonType: "date" }
        }
    }
};

function applySchema(name, validator) {
    const exists = db.getCollectionNames().includes(name);
    if (!exists) {
        db.createCollection(name, {
            validator,
            validationLevel: "moderate",
            validationAction: "error"
        });
        print(`Colección '${name}' creada`);
    } else {
        db.runCommand({
            collMod: name,
            validator,
            validationLevel: "moderate",
            validationAction: "error"
        });
        print(`Colección '${name}' ya existía (validator aplicado)`);
    }
}

applySchema("users",   userValidator);
applySchema("matches", matchValidator);

/* ============================================================
 * FASE 3 — Índices
 * ============================================================ */

// users
db.users.createIndex({ email: 1 },    { unique: true, name: "ux_users_email" });
db.users.createIndex({ username: 1 }, { unique: true, name: "ux_users_username" });
print("Índices de 'users' asegurados");

// matches
db.matches.createIndex({ player1: 1 },                          { name: "ix_matches_player1" });
db.matches.createIndex({ player2: 1 },                          { name: "ix_matches_player2" });
db.matches.createIndex({ status: 1 },                           { name: "ix_matches_status" });
db.matches.createIndex({ player1: 1, player2: 1, status: 1 },    { name: "ix_matches_players_status" });
print(" Índices de 'matches' asegurados");

print("──────────────────────────────────────────────");
print("[001] Migración completada\n");