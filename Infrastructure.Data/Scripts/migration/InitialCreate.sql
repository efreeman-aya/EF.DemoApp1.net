﻿IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'todo') IS NULL EXEC(N'CREATE SCHEMA [todo];');
GO

CREATE TABLE [todo].[SystemSetting] (
    [Id] uniqueidentifier NOT NULL,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(200) NULL,
    [Flags] int NOT NULL,
    [CreatedDate] datetime2(0) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [UpdatedDate] datetime2(0) NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_SystemSetting] PRIMARY KEY NONCLUSTERED ([Id])
);
GO

CREATE TABLE [todo].[TodoItem] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Status] int NOT NULL,
    [SecureRandom] nvarchar(100) NULL,
    [SecureDeterministic] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2(0) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [UpdatedDate] datetime2(0) NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TodoItem] PRIMARY KEY NONCLUSTERED ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_SystemSetting_Key] ON [todo].[SystemSetting] ([Key]);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_TodoItem_Name] ON [todo].[TodoItem] ([Name]);
GO

IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_WITH_AKV')
BEGIN
CREATE COLUMN MASTER KEY [CMK_WITH_AKV]
WITH (
    KEY_STORE_PROVIDER_NAME = N'AZURE_KEY_VAULT',
    KEY_PATH = N'https://dev-kv-a4.vault.azure.net/keys/SQL-ColMaskerKey-Default/3706efcbd65d4ed599d62a1dfa3e94b6',
    ENCLAVE_COMPUTATIONS (SIGNATURE = 0x3D580D75A9CD52EE93B5652E8840EAA261CA07505E1107765D14789F5071AB7E08172AF2839E55D40518BB36E37262F59F335CD758500AEA5DE9A6C7F1B5C9929E5EC26B34A6DEB9F72DD9DFDE667B3DF761BD1EB45E353D897A11BDCCFAD7143F1D506AFD702C1AF18000ABB908ACB6206CF60F89032F119CFF277938DB3105428EED5F49BA2A4E2E95C7AE1B1495EDF39BDC7BD22BB119BD1A0BF2E388FF2126CEADF1C59B8F9767121042C75E311C105D2C3DAF41E34409883BFE1417056F4BE4970E96F4FDC78A8B61811930EDF4E89FF62108C6B32EDD87F0FACCC4EE42E8B32C9130FD454B8EE57A830E19B67F0881EC8AA38CF71BD67F3B962A915008)
);
END
ELSE
BEGIN
    SELECT 'COLUMN MASTER KEY [CMK_WITH_AKV] exists.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_WITH_AKV')
BEGIN
CREATE COLUMN ENCRYPTION KEY [CEK_WITH_AKV] 
WITH VALUES (
    COLUMN_MASTER_KEY = [CMK_WITH_AKV],
    ALGORITHM = 'RSA_OAEP', 
    ENCRYPTED_VALUE = 0x01C0000001680074007400700073003A002F002F006400650076002D006B0076002D00610034002E007600610075006C0074002E0061007A007500720065002E006E00650074002F006B006500790073002F00730071006C002D0063006F006C006D00610073006B00650072006B00650079002D00640065006600610075006C0074002F003300370030003600650066006300620064003600350064003400650064003500390039006400360032006100310064006600610033006500390034006200360037E5842AA9282F3271D283DA04AF2D1DB4A8F465417E863D147337D1F54A3E17818208796DD9424124B75156B195BCF36AF69F4E070447F196D25F8AC9910A3C4FEE3E0D874073F4566891440A8614E2FD7D9493A36306987FA8700FE0FEB4A72B1FCC6A293413203EFE6F184AA6B031146B88754B039EA20AEEAD036A04F6785C60D3A8AFE727F2578B41DB37DAAD13394E880B3CF3F04E0E3BE58100CA1D15F181CF80BE8988FC81384FF876FB06411798384C3AE1BEE9A8589372ED59BFDB0BF060E043DB756A2DDD3BAC8E1D38E2007A642F3484AC79EA0B8D9C2F5330C3E35DF7EEDD28CEBBFCAC5BB1F98C98A63DE989C0A3FCCE9543D876F2333D5ED90F0917EB614E2061AD0F97BE64331A7AEB2B17B3E39606CBE22431B769A0F856B2034D5FE3F218383B84C7240AC47B4BF35738CBF44B285CBC7559B8A1391EB760EF6087D42C6A0D2F7CE334A7DD69FC346EC5E1C1FC3950B93BB094CC82AA63D9555E96449AA32A7CC54C0ADA791D86E19A6F9C7A5A36C3606A5A41B270F6479D5016C87C5972396A02A8C1C9B7C329BE3B6D6C0763ED9C3BAF01C7A56E28CC565650E757E4F02F6B48C3B9BCB864ACB4960830EAC74080C5A797C576BCE28A851C57E1D2751E652FEB1F162784321FBFE37E921E42D34A0320D7003E8F94E1DF8C2F39D130B337B340C862DA262AC826D6B9472304413F2FFBE6F765D541A4
);
END
ELSE
BEGIN
    SELECT 'COLUMN ENCRYPTION KEY [CEK_WITH_AKV] exists.';
END
GO

ALTER TABLE [todo].[TodoItem]
                                ALTER COLUMN [SecureDeterministic] nvarchar(100) 
                                COLLATE Latin1_General_BIN2 ENCRYPTED WITH(
                                        ENCRYPTION_TYPE = DETERMINISTIC, 
                                        ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                        COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
GO

ALTER TABLE [todo].[TodoItem]
                                ALTER COLUMN [SecureRandom] nvarchar(100) 
                                COLLATE Latin1_General_BIN2 ENCRYPTED WITH(
                                        ENCRYPTION_TYPE = RANDOMIZED, 
                                        ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                        COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20231217200249_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

