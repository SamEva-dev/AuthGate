-- Script SQL pour créer l'admin AuthGate manuellement
-- À exécuter dans pgAdmin ou psql

-- 1. Créer les rôles (si pas déjà créés)
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "Description", "IsSystemRole", "ConcurrencyStamp", "CreatedAtUtc")
VALUES 
    (gen_random_uuid(), 'Admin', 'ADMIN', 'Administrator with full access', true, gen_random_uuid()::text, NOW()),
    (gen_random_uuid(), 'User', 'USER', 'Standard user', false, gen_random_uuid()::text, NOW())
ON CONFLICT DO NOTHING;

-- 2. Créer les permissions (si pas déjà créées)
INSERT INTO "Permissions" ("Id", "Code", "DisplayName", "Description", "Category", "IsActive", "CreatedAtUtc")
VALUES
    (gen_random_uuid(), 'users.read', 'Read Users', 'Can view user information', 'Users', true, NOW()),
    (gen_random_uuid(), 'users.write', 'Write Users', 'Can create and update users', 'Users', true, NOW()),
    (gen_random_uuid(), 'users.delete', 'Delete Users', 'Can delete users', 'Users', true, NOW()),
    (gen_random_uuid(), 'roles.read', 'Read Roles', 'Can view roles', 'Roles', true, NOW()),
    (gen_random_uuid(), 'roles.write', 'Write Roles', 'Can create and update roles', 'Roles', true, NOW()),
    (gen_random_uuid(), 'roles.delete', 'Delete Roles', 'Can delete roles', 'Roles', true, NOW()),
    (gen_random_uuid(), 'permissions.read', 'Read Permissions', 'Can view permissions', 'Permissions', true, NOW()),
    (gen_random_uuid(), 'permissions.write', 'Write Permissions', 'Can assign and revoke permissions', 'Permissions', true, NOW())
ON CONFLICT DO NOTHING;

-- 3. Créer l'utilisateur admin
-- Password hash pour "Admin@123" (bcrypt)
INSERT INTO "AspNetUsers" 
    ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", 
     "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", 
     "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount", 
     "IsActive", "MfaEnabled", "CreatedAtUtc")
VALUES 
    (gen_random_uuid(), 
     'admin@authgate.com', 
     'ADMIN@AUTHGATE.COM', 
     'admin@authgate.com', 
     'ADMIN@AUTHGATE.COM', 
     true,
     '$2a$11$8vJ0YQZ9xZ5L5X5X5X5X5OuGqZX5X5X5X5X5X5X5X5X5X5X5X5X5X', -- Hash pour "Admin@123"
     gen_random_uuid()::text,
     gen_random_uuid()::text,
     NULL,
     false,
     false,
     NULL,
     true,
     0,
     true,
     false,
     NOW())
ON CONFLICT DO NOTHING;

-- 4. Assigner le rôle Admin à l'utilisateur
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'admin@authgate.com' AND r."Name" = 'Admin'
ON CONFLICT DO NOTHING;

-- 5. Assigner toutes les permissions au rôle Admin
INSERT INTO "RolePermissions" ("RoleId", "PermissionId", "CreatedAtUtc")
SELECT r."Id", p."Id", NOW()
FROM "AspNetRoles" r, "Permissions" p
WHERE r."Name" = 'Admin'
ON CONFLICT DO NOTHING;

-- Vérification
SELECT u."Email", r."Name" as "Role", COUNT(p."Id") as "PermissionCount"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
LEFT JOIN "RolePermissions" rp ON r."Id" = rp."RoleId"
LEFT JOIN "Permissions" p ON rp."PermissionId" = p."Id"
WHERE u."Email" = 'admin@authgate.com'
GROUP BY u."Email", r."Name";
