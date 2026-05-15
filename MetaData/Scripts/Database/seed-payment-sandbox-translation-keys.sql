-- Idempotent seed: payment sandbox UI strings (FE pulls via GET /api/v1/translations/{languageCode}).
-- Primary fix for raw keys like "payment.sandbox.pay" on the green demo button — rows must exist in amesa_content.translations.
-- Schema: amesa_content.translations (Content service).
--
-- Run: psql … -f MetaData/Scripts/Database/seed-payment-sandbox-translation-keys.sql
--   or: .\MetaData\Scripts\Database\run-seed-payment-sandbox-keys.ps1 -Apply
-- After run: invalidate Redis keys matching translations_* if Content caches bundles.

INSERT INTO amesa_content.translations (
    "Id",
    "LanguageCode",
    "Key",
    "Value",
    "Description",
    "Category",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "CreatedBy",
    "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    l."Code",
    s.key,
    CASE l."Code"
        WHEN 'en' THEN s.en
        WHEN 'es' THEN s.es
        WHEN 'fr' THEN s.fr
        WHEN 'pl' THEN s.pl
        WHEN 'he' THEN s.he
        WHEN 'ar' THEN s.ar
        ELSE s.en
    END,
    s.description,
    'Payment',
    true,
    NOW(),
    NOW(),
    'seed-payment-sandbox-keys',
    'seed-payment-sandbox-keys'
FROM amesa_content.languages AS l
CROSS JOIN (
    VALUES
        ('payment.sandbox.pay',
            'Sandbox pay',
            'Pagar (sandbox)',
            'Paiement sandbox',
            'Płatność sandbox',
            'תשלום ארגז חול',
            'دفع تجريبي',
            'Demo bypass button label'),
        ('payment.sandbox.notReady',
            'Wait for price to finish updating, then try again.',
            'Espere a que se actualice el precio e inténtelo de nuevo.',
            'Attendez la mise à jour du prix, puis réessayez.',
            'Poczekaj na aktualizację ceny i spróbuj ponownie.',
            'המתן לעדכון המחיר ונסה שוב.',
            'انتظر حتى يكتمل تحديث السعر ثم أعد المحاولة.',
            'Toast when sandbox pay clicked too early'),
        ('payment.sandbox.failed',
            'Sandbox payment could not complete.',
            'No se pudo completar el pago sandbox.',
            'Le paiement sandbox n''a pas pu aboutir.',
            'Płatność sandbox nie mogła zostać zakończona.',
            'לא ניתן להשלים את תשלום ארגז החול.',
            'تعذر إتمام الدفع التجريبي.',
            'Sandbox purchase error'),
        ('payment.stripe.invalidState',
            'Please complete quantity selection first.',
            'Complete primero la selección de cantidad.',
            'Veuillez d''abord terminer la sélection de la quantité.',
            'Najpierw dokończ wybór ilości.',
            'יש להשלים תחילה את בחירת הכמות.',
            'يرجى إكمال اختيار الكمية أولاً.',
            'Stripe tab blocked message'),
        ('payment.crypto.invalidState',
            'Please complete quantity selection first.',
            'Complete primero la selección de cantidad.',
            'Veuillez d''abord terminer la sélection de la quantité.',
            'Najpierw dokończ wybór ilości.',
            'יש להשלים תחילה את בחירת הכמות.',
            'يرجى إكمال اختيار الكمية أولاً.',
            'Crypto tab blocked message')
) AS s(key, en, es, fr, pl, he, ar, description)
WHERE l."IsActive" = true
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
