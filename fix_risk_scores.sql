-- Mevcut risk skorlarını 0-10 arasına normalize et
-- (0-100 arası olan skorları 10'a böl)

UPDATE "DocumentAnalyses"
SET "RiskScore" = "RiskScore" / 10
WHERE "RiskScore" > 10;

-- Sonuçları kontrol et
SELECT
    da."Id",
    td."FileName",
    td."DocumentType",
    da."RiskScore",
    da."RiskLevel"
FROM "DocumentAnalyses" da
JOIN "TenderDocuments" td ON td."Id" = da."DocumentId"
ORDER BY da."CreatedAt" DESC;
