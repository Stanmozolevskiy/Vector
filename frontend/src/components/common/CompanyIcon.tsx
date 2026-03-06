import { siGoogle, siMeta, siApple, siNetflix, siStripe } from 'simple-icons';

type CompanyIconProps = {
  company: string;
  size?: number;
  className?: string;
  title?: string;
};

const normalizeCompany = (value: string) => value.trim().toLowerCase();

const getCompanyIcon = (company: string) => {
  const key = normalizeCompany(company);
  if (key === 'google') return siGoogle;
  if (key === 'meta' || key === 'facebook') return siMeta;
  if (key === 'apple') return siApple;
  if (key === 'netflix') return siNetflix;
  if (key === 'stripe') return siStripe;
  return null;
};

const getInitial = (company: string) => {
  const trimmed = company.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : '?';
};

export const CompanyIcon = ({ company, size = 18, className, title }: CompanyIconProps) => {
  const normalized = normalizeCompany(company);
  if (normalized === 'amazon') {
    return (
      <img
        src="https://www.amazon.com/favicon.ico"
        width={size}
        height={size}
        alt={title || company}
        title={title || company}
        className={className}
        loading="lazy"
        referrerPolicy="no-referrer"
        style={{ width: size, height: size, objectFit: 'contain', display: 'block' }}
      />
    );
  }

  if (normalized === 'microsoft') {
    const tile = Math.max(3, Math.round(size / 2.4));
    const gap = Math.max(1, Math.round(size / 7));
    const total = tile * 2 + gap;
    // viewBox uses our computed grid units; width/height props scale it visually

    return (
      <svg
        width={size}
        height={size}
        viewBox={`0 0 ${total} ${total}`}
        role="img"
        aria-label={title || company}
        className={className}
      >
        <title>{title || company}</title>
        <rect x="0" y="0" width={tile} height={tile} fill="#F25022" />
        <rect x={tile + gap} y="0" width={tile} height={tile} fill="#7FBA00" />
        <rect x="0" y={tile + gap} width={tile} height={tile} fill="#00A4EF" />
        <rect x={tile + gap} y={tile + gap} width={tile} height={tile} fill="#FFB900" />
      </svg>
    );
  }

  const icon = getCompanyIcon(company);
  if (!icon) {
    return (
      <span className={className} aria-label={title || company} title={title || company}>
        {getInitial(company)}
      </span>
    );
  }

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      role="img"
      aria-label={title || company}
      className={className}
      style={{ color: `#${icon.hex}` }}
    >
      <title>{title || company}</title>
      <path d={icon.path} fill="currentColor" />
    </svg>
  );
};

