/**
 * DashboardWidgets.jsx
 * Shared, lightweight dashboard components — no external chart library.
 * Pure SVG + CSS animations.
 */

import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import './DashboardWidgets.css';

// ── Sparkline (mini line chart) ──────────────────────────────────────
export function Sparkline({ data = [], color = 'var(--primary)', height = 40, width = 80 }) {
  if (!data || data.length < 2) return null;

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;

  const points = data.map((v, i) => {
    const x = (i / (data.length - 1)) * width;
    const y = height - ((v - min) / range) * (height - 4) - 2;
    return `${x},${y}`;
  });

  const pathD = `M ${points.join(' L ')}`;
  const areaD = `M ${points[0]} L ${points.join(' L ')} L ${width},${height} L 0,${height} Z`;

  return (
    <svg
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      className="sparkline"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id={`sg-${color.replace(/[^a-z0-9]/gi, '')}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.25" />
          <stop offset="100%" stopColor={color} stopOpacity="0.02" />
        </linearGradient>
      </defs>
      {/* Area fill */}
      <path
        d={areaD}
        fill={`url(#sg-${color.replace(/[^a-z0-9]/gi, '')})`}
      />
      {/* Line */}
      <path
        d={pathD}
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      {/* Last point dot */}
      <circle
        cx={points[points.length - 1].split(',')[0]}
        cy={points[points.length - 1].split(',')[1]}
        r="3"
        fill={color}
      />
    </svg>
  );
}

// ── Donut Chart ──────────────────────────────────────────────────────
export function DonutChart({ segments = [], size = 120, thickness = 18, label, sublabel }) {
  const total = segments.reduce((s, seg) => s + (seg.value || 0), 0);
  if (total === 0) return null;

  const r = (size - thickness) / 2;
  const cx = size / 2;
  const cy = size / 2;
  const circumference = 2 * Math.PI * r;

  let offset = 0;
  const arcs = segments.map((seg) => {
    const pct = seg.value / total;
    const dash = pct * circumference;
    const gap = circumference - dash;
    const arc = { ...seg, dash, gap, offset };
    offset += dash;
    return arc;
  });

  return (
    <div className="donut-chart-wrap" style={{ width: size, height: size }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} aria-hidden="true">
        {/* Background track */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none"
          stroke="var(--surface-container-high)"
          strokeWidth={thickness}
        />
        {/* Segments */}
        {arcs.map((arc, i) => (
          <circle
            key={i}
            cx={cx} cy={cy} r={r}
            fill="none"
            stroke={arc.color}
            strokeWidth={thickness}
            strokeDasharray={`${arc.dash} ${arc.gap}`}
            strokeDashoffset={-arc.offset}
            strokeLinecap="butt"
            style={{
              transform: 'rotate(-90deg)',
              transformOrigin: `${cx}px ${cy}px`,
              transition: 'stroke-dasharray 0.6s ease',
            }}
          />
        ))}
      </svg>
      {/* Center label */}
      {(label || sublabel) && (
        <div className="donut-center">
          {label && <span className="donut-label">{label}</span>}
          {sublabel && <span className="donut-sublabel">{sublabel}</span>}
        </div>
      )}
    </div>
  );
}

// ── Trend Badge ──────────────────────────────────────────────────────
export function TrendBadge({ value, suffix = '%', inverse = false }) {
  if (value === null || value === undefined) return null;

  const isPositive = inverse ? value < 0 : value > 0;
  const isNeutral = value === 0;

  return (
    <span className={`trend-badge ${isNeutral ? 'neutral' : isPositive ? 'positive' : 'negative'}`}>
      {isNeutral ? (
        <Minus size={11} />
      ) : isPositive ? (
        <TrendingUp size={11} />
      ) : (
        <TrendingDown size={11} />
      )}
      {Math.abs(value)}{suffix}
    </span>
  );
}

// ── Stat Card (enhanced) ─────────────────────────────────────────────
export function StatCard({
  label,
  value,
  sublabel,
  icon: Icon,
  iconBg = 'var(--primary-fixed)',
  iconColor = 'var(--primary)',
  trend,
  trendInverse = false,
  sparkData,
  sparkColor,
  badge,
  badgeVariant = 'warning',
  onClick,
  accent,
}) {
  return (
    <div
      className={`dash-stat-card ${onClick ? 'clickable' : ''} ${accent ? `dash-stat-accent-${accent}` : ''}`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
    >
      {/* Top row: icon + sparkline */}
      <div className="dsc-top">
        <div className="dsc-icon" style={{ background: iconBg, color: iconColor }}>
          <Icon size={22} />
        </div>
        {sparkData && (
          <Sparkline
            data={sparkData}
            color={sparkColor || iconColor}
            height={36}
            width={72}
          />
        )}
      </div>

      {/* Value */}
      <div className="dsc-value">{value}</div>

      {/* Label + trend */}
      <div className="dsc-bottom">
        <span className="dsc-label">{label}</span>
        {trend !== undefined && (
          <TrendBadge value={trend} inverse={trendInverse} />
        )}
      </div>

      {/* Sublabel */}
      {sublabel && <p className="dsc-sublabel">{sublabel}</p>}

      {/* Badge (e.g. "3 Pending") */}
      {badge && (
        <span className={`dsc-badge badge-${badgeVariant}`}>{badge}</span>
      )}
    </div>
  );
}

// ── Action Card (enhanced) ───────────────────────────────────────────
export function ActionCard({ icon: Icon, iconBg, iconColor, title, desc, badge, badgeVariant = 'error', onClick }) {
  return (
    <button className="dash-action-card" onClick={onClick}>
      <div className="dac-icon-wrap" style={{ background: iconBg || 'var(--primary-fixed)', color: iconColor || 'var(--primary)' }}>
        <Icon size={26} />
        {badge && (
          <span className={`dac-badge badge-${badgeVariant}`}>{badge}</span>
        )}
      </div>
      <h3 className="dac-title">{title}</h3>
      <p className="dac-desc">{desc}</p>
    </button>
  );
}

// ── Mini Bar Chart ───────────────────────────────────────────────────
export function MiniBarChart({ data = [], color = 'var(--primary)', height = 60 }) {
  if (!data.length) return null;
  const max = Math.max(...data.map(d => d.value || 0)) || 1;

  return (
    <div className="mini-bar-chart" style={{ height }}>
      {data.map((d, i) => (
        <div key={i} className="mbc-bar-wrap" title={`${d.label}: ${d.value}`}>
          <div
            className="mbc-bar"
            style={{
              height: `${((d.value || 0) / max) * 100}%`,
              background: d.color || color,
              animationDelay: `${i * 60}ms`,
            }}
          />
          {d.label && <span className="mbc-label">{d.label}</span>}
        </div>
      ))}
    </div>
  );
}

// ── Date Range Tabs ──────────────────────────────────────────────────
export function DateRangeTabs({ value, onChange }) {
  const options = [
    { label: 'Today', value: 'today' },
    { label: 'Week', value: 'week' },
    { label: 'Month', value: 'month' },
    { label: 'All', value: 'all' },
  ];
  return (
    <div className="date-range-tabs" role="group" aria-label="Date range">
      {options.map(opt => (
        <button
          key={opt.value}
          className={`drt-btn ${value === opt.value ? 'active' : ''}`}
          onClick={() => onChange(opt.value)}
          aria-pressed={value === opt.value}
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}

// ── Section Header ───────────────────────────────────────────────────
export function DashSectionHeader({ title, subtitle, action, actionLabel }) {
  return (
    <div className="dash-section-header">
      <div>
        <h2 className="dash-section-title">{title}</h2>
        {subtitle && <p className="dash-section-sub">{subtitle}</p>}
      </div>
      {action && (
        <button className="btn btn-ghost btn-sm" onClick={action}>
          {actionLabel || 'View All'}
        </button>
      )}
    </div>
  );
}

// ── Orders Table (enhanced) ──────────────────────────────────────────
export function DashOrdersTable({ orders, columns, onRowClick, emptyMessage = 'No orders found' }) {
  if (!orders?.length) {
    return (
      <div className="dash-table-empty">
        <p>{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="dash-table-wrap">
      <table className="dash-table">
        <thead>
          <tr>
            {columns.map(col => (
              <th key={col.key} style={{ width: col.width }}>{col.label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {orders.map((row, i) => (
            <tr
              key={row.id || i}
              onClick={() => onRowClick?.(row)}
              className={onRowClick ? 'clickable' : ''}
              style={{ animationDelay: `${i * 30}ms` }}
            >
              {columns.map(col => (
                <td key={col.key}>
                  {col.render ? col.render(row) : row[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
