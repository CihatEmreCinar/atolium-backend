# Assets

Base.html ve Ticket.html, logo/ikon gibi statik görselleri **hosted URL** olarak
referans alır (CDN üzerinden `LogoUrl`, `InfoIconUrl`, hero/badge ikonları —
bkz. `Infrastructure/EmailBrandOptions.cs`). Bu, e-posta istemcileri arasında
en yüksek uyumluluğu sağlayan ve boyutu küçük tutan yaklaşımdır.

Bu klasördeki alt dizinler, gerçek dosyaları CDN'e yüklemeden önce
organize etmek için ayrılmıştır:

- `Logo/` — Atolium logosu (farklı boyutlar)
- `Icons/` — Info kutusu, hero ikonları vb.
- `HeroIllustrations/` — Achievement/Certificate gibi e-postalarda kullanılacak illüstrasyonlar
- `SocialIcons/` — Footer'a sosyal medya ikonları eklenecekse

QR kod gibi **kişiye özel** görseller (Ticket) burada tutulmaz; bunlar
event ile birlikte `byte[]` olarak taşınır ve `EmailInlineImage` aracılığıyla
`cid:` ile e-postaya gömülür (bkz. `Builders/CommerceEmailBuilders.cs`).
