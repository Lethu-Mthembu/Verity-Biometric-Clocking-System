import user from "../../assests/user.svg";

export default function Avatar({ small = false }) {
  return (
    <div className='grid shrink-0 place-items-center rounded-full'>
      <img src={user} alt="" className={small ? 'h-10 w-10' : 'h-10 w-10'} /> 
    </div>
  )
}
