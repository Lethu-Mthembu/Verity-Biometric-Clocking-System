import user from "../../assests/user.svg";
const colors = {
  violet: 'bg-violet-600',
  sky: 'bg-sky-700',
  rose: 'bg-rose-600',
  amber: 'bg-amber-600',
  emerald: 'bg-emerald-700'
}

export default function Avatar({ color = 'sky', small = false }) {
  return (
    <div className='grid shrink-0 place-items-center rounded-full'>
      <img src={user} alt="" className={small ? 'h-10 w-10' : 'h-10 w-10'} /> 
    </div>
  )
}
