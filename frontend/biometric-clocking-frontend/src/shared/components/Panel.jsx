export default function Panel({ children, className = '' }) {
  return (
    <section className={`rounded-xl bg-[#122a47] ${className}`}>
      {children}
    </section>
  )
}