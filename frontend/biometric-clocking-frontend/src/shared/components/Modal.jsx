export default function Modal({ children, onClose }) {
  return <div className="fixed inset-0 z-50 grid place-items-center bg-[#06101de6] p-5 backdrop-blur-sm">
    <section className="relative w-full max-w-md rounded-2xl bg-[#10233a] p-7 shadow-2xl shadow-black/50">
      <button onClick={onClose} aria-label="Close" className="absolute right-4 top-4 rounded-lg px-2 py-1 text-lg leading-none text-slate-400 hover:bg-slate-700 hover:text-white">
        ×
      </button>
      {children}
    </section>
  </div>
}